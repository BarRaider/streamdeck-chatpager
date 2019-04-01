using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Text;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using System.Linq;

namespace ChatPager.Twitch
{
    public class TwitchChat
    {

        #region Private Members

        private static TwitchChat instance = null;
        private static readonly object objLock = new object();

        private TwitchClient client = new TwitchClient();
        private TwitchToken token = null;
        private int pageCooldown;
        private DateTime lastPage;
        private List<string> allowedPagers;

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

        public event EventHandler PageRaised;

        public bool IsConnected
        {
            get
            {
                return client.IsConnected;
            }
        }

        #endregion


        private TwitchChat()
        {
            client.OnConnected += Client_OnConnected;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnDisconnected += Client_OnDisconnected;
            TwitchTokenManager.Instance.TokensChanged += Instance_TokensChanged;
            token = TwitchTokenManager.Instance.GetToken();
        }

        public void Initalize(int pageCooldown, List<string> allowedPagers)
        {
            if (allowedPagers != null)
            {
                this.allowedPagers = allowedPagers.Select(x => x.ToLowerInvariant()).ToList();
            }
            this.pageCooldown = pageCooldown;
        }

       
        public void Connect()
        {
            Disconnect(); // Disconnect if already conected with previous credentials

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
            client.Initialize(credentials, username);
            client.Connect();
        }

        public void Disconnect()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "TwitchChat: Attempting to disconnect");
            if (client != null && client.IsConnected)
            {
                client.Disconnect();
            }
        }

        #region Private Members

        private void Client_OnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            ParseCommand(e.ChatMessage);
        }

        private void ParseCommand(ChatMessage msg)
        {
            var words = msg.Message.ToLowerInvariant().Split(' ');
            if (words[0] == "!page")
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"{msg.DisplayName} requested a page");
                if (PageRaised != null)
                {
                    if ((DateTime.Now - lastPage).TotalSeconds > pageCooldown)
                    {
                        if (allowedPagers == null || allowedPagers.Count == 0 || allowedPagers.Contains(msg.DisplayName.ToLowerInvariant()))
                        {
                            lastPage = DateTime.Now;
                            PageRaised?.Invoke(this, EventArgs.Empty);
                            client.SendMessage(msg.Channel, $"Hey, @{msg.DisplayName}, I am now getting paged...!  \r\n(Get a pager for your Elgato Stream Deck at https://barraider.github.io )");
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
            }
        }

        private void Client_OnConnected(object sender, TwitchLib.Client.Events.OnConnectedArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Connected to chat room: {e.AutoJoinChannel}");
        }

        private void Client_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Disconnected from chat room");
        }

        private void Instance_TokensChanged(object sender, TwitchTokenEventArgs e)
        {
            token = e.Token;

            Connect();
        }

        #endregion


    }
}
