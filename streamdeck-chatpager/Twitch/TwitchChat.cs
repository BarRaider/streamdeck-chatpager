using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Text;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using System.Linq;
using ChatPager.Wrappers;

namespace ChatPager.Twitch
{
    public class TwitchChat
    {

        #region Private Members
        private const string DEFAULT_CHAT_MESSAGE = "Hey, {USERNAME}, I am now getting paged...! (Get a pager for your Elgato Stream Deck at https://BarRaider.com )";
        private const string COMMAND_STARTWITH_PREFIX = @"/PRIVMSG";
        private const string COMMAND_PREFIX = COMMAND_STARTWITH_PREFIX + " #{0} :";
        


        private static TwitchChat instance = null;
        private static readonly object objLock = new object();

        private const string DEFAULT_PAGE_COMMAND = "page";

        private TwitchClient client;
        private TwitchToken token = null;
        private int pageCooldown;
        private DateTime lastPage;
        private List<string> allowedPagers;
        private List<string> monitoredStreamers;
        private DateTime lastConnectAttempt;
        private readonly object initLock = new object();

        private Dictionary<string, DateTime> dictUserMessages = new Dictionary<string, DateTime>();

        #endregion

        #region Constructors

        public static TwitchChat Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                lock (objLock)
                {
                    if (instance == null)
                    {
                        instance = new TwitchChat();
                    }
                    return instance;
                }
            }
        }

        #endregion

        #region Public Members

        public event EventHandler<PageRaisedEventArgs> PageRaised;
        public event EventHandler<ChatMessageReceivedEventArgs> OnChatMessageReceived;

        public bool IsConnected
        {
            get
            {
                return client.IsConnected;
            }
        }

        public string ChatMessage { get; private set; }
        public string PageCommand { get; private set; }

        #endregion


        private TwitchChat()
        {
            ChatMessage = DEFAULT_CHAT_MESSAGE;
            PageCommand = DEFAULT_PAGE_COMMAND;
            ResetClient();
            TwitchTokenManager.Instance.TokensChanged += Instance_TokensChanged;
            token = TwitchTokenManager.Instance.GetToken();
        }

        #region Public Methods

        public void Initialize()
        {
            lock (initLock)
            {
                try
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, "TwitchChat: Initializing");
                    if (!client.IsConnected)
                    {
                        Logger.Instance.LogMessage(TracingLevel.INFO, "TwitchChat: Connecting to Chat");
                        Connect(DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"TwitchChat: Initialize exception {ex}");
                }
            }
        }

        public void InitializePager(int pageCooldown, List<string> allowedPagers, List<string> monitoredStreamers)
        {
            try
            {
                this.monitoredStreamers = null;
                this.allowedPagers = null;
                Logger.Instance.LogMessage(TracingLevel.INFO, "TwitchChat: Initializing Pager");
                if (allowedPagers != null)
                {
                    this.allowedPagers = allowedPagers.Select(x => x.ToLowerInvariant()).ToList();
                }
                if (monitoredStreamers != null)
                {
                    this.monitoredStreamers = monitoredStreamers.Select(x => x.ToLowerInvariant()).ToList();
                }
                this.pageCooldown = pageCooldown;
                Initialize();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"TwitchChat: Initialize Pager exception {ex}");
            }
        }

        public void SetChatMessage(string message)
        {
            ChatMessage = message;
        }

        public void SetPageCommand(string pageCommand)
        {
            if (!String.IsNullOrEmpty(pageCommand))
            {
                PageCommand = pageCommand.ToLowerInvariant().Replace("!", "");
            }
        }

        public void SendMessage(string chatMessage)
        {
            SendMessage(TwitchTokenManager.Instance.User?.UserName, chatMessage);
        }

        public void SendMessage(string channel, string chatMessage)
        {
            if (String.IsNullOrEmpty(channel) || String.IsNullOrEmpty(chatMessage))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"TwitchChat SendMessage called but channel or message are empty. Channel: {channel} Message: {chatMessage}");
                return;
            }

            if (!IsConnected)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"TwitchChat SendMessage called but not connected.");
                return;
            }

            // Attempt to join the channel if we're not in it - before sending the message.
            if (!client.JoinedChannels.Any(c => c.Channel.ToLowerInvariant() == channel.ToLowerInvariant()))
            {
                client.JoinChannel(channel);
            }

            if (chatMessage[0] == '/')
            {
                if (!chatMessage.StartsWith(COMMAND_STARTWITH_PREFIX))
                {
                    chatMessage = COMMAND_PREFIX.Replace("{0}", channel) + chatMessage;
                }
                client.SendRaw(chatMessage.Substring(1));
            }
            else
            {
                client.SendMessage(channel, chatMessage);
            }
        }

        public List<string> GetLastChatters()
        {
            try
            {
                return dictUserMessages.Keys.Select(k => new { Username = k, LastChat = dictUserMessages[k] }).OrderByDescending(o => o.LastChat).Select(o => o.Username).ToList();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetLastChatters Exception {ex}");
            }
            return null;
        }

        #endregion

        #region Private Methods

        private void Connect(DateTime connectRequestTime)
        {
            lock (objLock)
            {
                try
                {
                    if ((lastConnectAttempt > connectRequestTime) || ((connectRequestTime - lastConnectAttempt).TotalSeconds < 2)) // Prevent spamming Twitch
                    {
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"TwitchChat: Connected recently");
                        return;
                    }
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"TwitchChat: Connect called");
                    Disconnect(); // Disconnect if already connected with previous credentials

                    if (token == null || String.IsNullOrWhiteSpace(token.Token))
                    {
                        Logger.Instance.LogMessage(TracingLevel.INFO, "TwitchChat: Cannot connect, invalid token");
                        return;
                    }

                    if (TwitchTokenManager.Instance.User == null || String.IsNullOrWhiteSpace(TwitchTokenManager.Instance.User.UserName))
                    {
                        Logger.Instance.LogMessage(TracingLevel.INFO, "TwitchChat: Cannot connect, invalid user object");
                        return;
                    }

                    Logger.Instance.LogMessage(TracingLevel.INFO, "TwitchChat: Attempting to connect");
                    string username = TwitchTokenManager.Instance.User.UserName;
                    ConnectionCredentials credentials = new ConnectionCredentials(username, $"oauth:{token.Token}");
                    dictUserMessages = new Dictionary<string, DateTime>();
                    ResetClient();
                    client.Initialize(credentials, username);
                    client.Connect();
                    lastConnectAttempt = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"TwitchChat: Connect exception {ex}");
                }
            }
        }

        private void Disconnect()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "TwitchChat: Attempting to disconnect");
            if (client != null && client.IsConnected)
            {
                client.Disconnect();
            }
        }

        private void HandleReceivedMessage(ChatMessage msg)
        {
            OnChatMessageReceived?.Invoke(this, new ChatMessageReceivedEventArgs(msg.Message, msg.Username));
            string userName = msg.Username.ToLowerInvariant();
            dictUserMessages[userName] = DateTime.Now;
        }

        private void ParseCommand(ChatCommand cmd)
        {
            var msg = cmd.ChatMessage;
            if (cmd.CommandText.ToLowerInvariant() == PageCommand)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"{msg.DisplayName} requested a page");
                if (PageRaised != null)
                {
                    if ((DateTime.Now - lastPage).TotalSeconds > pageCooldown)
                    {
                        if (allowedPagers == null || allowedPagers.Count == 0 || allowedPagers.Contains(msg.DisplayName.ToLowerInvariant()))
                        {
                            lastPage = DateTime.Now;
                            PageRaised?.Invoke(this, new PageRaisedEventArgs(cmd.ArgumentsAsString));

                            if (!String.IsNullOrWhiteSpace(ChatMessage))
                            {
                                string chatMessage = ChatMessage.Replace("{USERNAME}", $"@{msg.DisplayName}");
                                client.SendMessage(msg.Channel, chatMessage);
                            }
                        }
                        else
                        {
                            Logger.Instance.LogMessage(TracingLevel.INFO, $"Cannot page, user {msg.DisplayName} is not allowed to page");
                        }
                    }
                    else
                    {
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"Cannot page, cooldown enabled");
                    }
                }
                else
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Cannot page, no plugin is currently enabled");
                }
            }
        }

        private void Client_OnConnected(object sender, TwitchLib.Client.Events.OnConnectedArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Connected to chat room: {e.AutoJoinChannel}");

            if (monitoredStreamers != null)
            {
                foreach (var streamer in monitoredStreamers)
                {
                    client.JoinChannel(streamer);
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Connecting to chat room: {streamer}");
                }
            }
        }

        private void Client_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Disconnected from chat room");
        }

        private void Instance_TokensChanged(object sender, TwitchTokenEventArgs e)
        {
            token = e.Token;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Tokens changed, reconnecting");
            Connect(DateTime.Now);
        }

        private void ResetClient()
        {
            if (client != null)
            {
                client.OnConnected -= Client_OnConnected;
                client.OnDisconnected -= Client_OnDisconnected;
                client.OnChatCommandReceived -= Client_OnChatCommandReceived;
                client.OnMessageReceived -= Client_OnMessageReceived;
                client.OnRaidNotification -= Client_OnRaidNotification;
                client.OnNewSubscriber -= Client_OnNewSubscriber;
                //client.OnUserJoined -= Client_OnUserJoined;
                //client.OnUserLeft -= Client_OnUserLeft;
                client.OnConnectionError -= Client_OnConnectionError;
                client.OnError -= Client_OnError;

            }
            client = null;
            client = new TwitchClient();
            client.OnConnected += Client_OnConnected;
            client.OnDisconnected += Client_OnDisconnected;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnRaidNotification += Client_OnRaidNotification;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            //client.OnUserJoined += Client_OnUserJoined;
            //client.OnUserLeft += Client_OnUserLeft;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnError += Client_OnError;

            // TODO -= these
            /*client.OnCommunitySubscription += Client_OnCommunitySubscription;
            client.OnHostingStarted += Client_OnHostingStarted;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnRaidNotification += Client_OnRaidNotification;
            client.OnUserStateChanged += Client_OnUserStateChanged;
            client.OnWhisperReceived += Client_OnWhisperReceived;
            */
        }

        private void Client_OnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            HandleReceivedMessage(e.ChatMessage);
        }

        private void Client_OnWhisperReceived(object sender, TwitchLib.Client.Events.OnWhisperReceivedArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"***Whisper: {e.WhisperMessage.Username}: {e.WhisperMessage.Message}");
        }

        private void Client_OnUserStateChanged(object sender, TwitchLib.Client.Events.OnUserStateChangedArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"***UserState: {e.UserState.DisplayName} {e.UserState.UserType}");
        }

        private void Client_OnRaidNotification(object sender, TwitchLib.Client.Events.OnRaidNotificationArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"***Raid: {e.RaidNotificaiton.DisplayName}");
            string userName = e.RaidNotificaiton.DisplayName.ToLowerInvariant();
            dictUserMessages[userName] = DateTime.Now;
        }

        private void Client_OnNewSubscriber(object sender, TwitchLib.Client.Events.OnNewSubscriberArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"***NewSubscriber: {e.Subscriber.DisplayName}");
            string userName = e.Subscriber.DisplayName.ToLowerInvariant();
            dictUserMessages[userName] = DateTime.Now;
        }

        private void Client_OnHostingStarted(object sender, TwitchLib.Client.Events.OnHostingStartedArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"***Hosting Started: {e.HostingStarted.HostingChannel}");
        }

        private void Client_OnCommunitySubscription(object sender, TwitchLib.Client.Events.OnCommunitySubscriptionArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"***CommunitySubscription: {e.GiftedSubscription.DisplayName}");
        }

        private void Client_OnError(object sender, TwitchLib.Communication.Events.OnErrorEventArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.WARN, $"TwitchChat Error: {e.Exception}");
        }

        private void Client_OnConnectionError(object sender, TwitchLib.Client.Events.OnConnectionErrorArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.WARN, $"TwitchChat Connection Error: {e.Error.Message}");
        }

        private void Client_OnChatCommandReceived(object sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {
            ParseCommand(e.Command);
        }

        private void Client_OnUserLeft(object sender, TwitchLib.Client.Events.OnUserLeftArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"User left channel: {e.Username}");
        }

        private void Client_OnUserJoined(object sender, TwitchLib.Client.Events.OnUserJoinedArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"User joined channel: {e.Username}");
        }

        #endregion


    }
}
