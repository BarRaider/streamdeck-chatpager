using BarRaider.SdTools;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.PubSub;

namespace ChatPager.Twitch
{

    //---------------------------------------------------
    //          BarRaider's Hall Of Fame
    // Subscriber: icessassin
    // Subscriber: nubby_ninja
    // Subscriber: Nachtmeister666 
    // Subscriber: CyberlightGames
    // Subscriber: Krinkelschmidt
    // Subscriber: iMackk
    // 200 Bits: Nachtmeister666
    // 10000 Bits: nubby_ninja
    // Subscriber: transparentpixel
    // nubby_ninja - 10 Gifted Subs
    // 5 Bits: Nachtmeister666
    // Subscriber: Sokren
    //---------------------------------------------------
    class TwitchPubSubManager
    {
        class PSLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (formatter == null)
                {
                    BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.WARN, "Formatter is null"); 
                    return;
                }

                var message = formatter(state, exception);

                if (!string.IsNullOrEmpty(message) || exception != null)
                {
                    BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, $"Pubsub internal log: {message} {exception}");
                }
            }
        }


        #region Private Members

        private static TwitchPubSubManager instance = null;
        private static readonly object objLock = new object();
        private static readonly HashSet<string> pastFollowers = new HashSet<string>();

        private TwitchPubSub pubsub;
        private bool isConnected = false;
        private bool connectCalled = false;
        private TwitchGlobalSettings global;
        private string channelName;

        #endregion

        #region Constructors

        public static TwitchPubSubManager Instance
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
                        instance = new TwitchPubSubManager();
                    }
                    return instance;
                }
            }
        }


        private TwitchPubSubManager()
        {
            GlobalSettingsManager.Instance.OnReceivedGlobalSettings += Global_OnReceivedGlobalSettings;
            GlobalSettingsManager.Instance.RequestGlobalSettings();
        }

        #endregion

        #region Public Methods

        public void Initialize()
        {
            if (global != null && !global.PubsubNotifications)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"PubSub Initialize called but PubSub notifications are disabled");
                return;
            }

            if (pubsub != null)
            {
                return;
            }

            isConnected = false;
            connectCalled = false;
            pubsub = new TwitchPubSub(new PSLogger<TwitchPubSub>());

            TwitchTokenManager.Instance.TokensChanged += Instance_TokensChanged;
            pubsub.OnPubSubServiceConnected += Pubsub_OnPubSubServiceConnected;
            pubsub.OnListenResponse += Pubsub_OnListenResponse;
            pubsub.OnStreamDown += Pubsub_OnStreamDown;
            pubsub.OnStreamUp += Pubsub_OnStreamUp;
            pubsub.OnBitsReceived += Pubsub_OnBitsReceived;
            pubsub.OnChannelSubscription += Pubsub_OnChannelSubscription;
            pubsub.OnChannelPoints += Pubsub_OnChannelPoints;
            pubsub.OnFollow += Pubsub_OnFollow;

            ConnectPubSub();
        }

        public void Disconnect()
        {
            if (pubsub == null)
            {
                return;
            }

            Logger.Instance.LogMessage(TracingLevel.INFO, "PubSub Disconnecting");
            TwitchTokenManager.Instance.TokensChanged -= Instance_TokensChanged;
            pubsub.OnPubSubServiceConnected -= Pubsub_OnPubSubServiceConnected;
            pubsub.OnListenResponse -= Pubsub_OnListenResponse;
            pubsub.OnStreamDown -= Pubsub_OnStreamDown;
            pubsub.OnStreamUp -= Pubsub_OnStreamUp;
            pubsub.OnBitsReceived -= Pubsub_OnBitsReceived;
            pubsub.OnChannelSubscription -= Pubsub_OnChannelSubscription;
            pubsub.OnChannelPoints -= Pubsub_OnChannelPoints;
            pubsub.OnFollow -= Pubsub_OnFollow;

            try
            {
                pubsub.Disconnect();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"PubSub Disconnect Exception: {ex}");
            }
            isConnected = false;
            pubsub = null;
        }

        #endregion

        #region Private Methods

        private void RegisterEventsToListenTo()
        {
            if (isConnected)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"RegisterEventsToListenTo called but already connected");
                return;
            }

            if (!TwitchTokenManager.Instance.TokenExists)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"RegisterEventsToListenTo called but token does not exist");
                return;
            }

            if (TwitchTokenManager.Instance.User == null || String.IsNullOrEmpty(TwitchTokenManager.Instance.User.UserName))
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"RegisterEventsToListenTo called but user info does not exist");
                Thread.Sleep(5000);

                if (TwitchTokenManager.Instance.User == null || String.IsNullOrEmpty(TwitchTokenManager.Instance.User.UserName))
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"RegisterEventsToListenTo called but user info does not exist! Cannot register for events");
                    return;
                }
            }
            
            isConnected = true;
            channelName = TwitchTokenManager.Instance.User.UserId;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"PubSub registering to events for {TwitchTokenManager.Instance.User.UserName} ({channelName})");
            var token = TwitchTokenManager.Instance.GetToken();

            pubsub.ListenToBitsEvents(channelName);
            pubsub.ListenToFollows(channelName);
            pubsub.ListenToSubscriptions(channelName);
            pubsub.ListenToChannelPoints(channelName);
            pubsub.SendTopics($"{token.Token}");

            Logger.Instance.LogMessage(TracingLevel.INFO, $"PubSub SendTopics oauth length {token.Token.Length}");
        }

        private void ConnectPubSub()
        {
            if (global != null && !global.PubsubNotifications)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"ConnectPubSub called but PubSub notifications are disabled");
                return;
            }

            if (connectCalled)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"ConnectPubSub called but already connected");
                return;
            }

            if (!TwitchTokenManager.Instance.TokenExists)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"ConnectPubSub called but token does not exist");
                return;
            }

            Logger.Instance.LogMessage(TracingLevel.INFO, $"PubSub connecting...");
            pubsub.Connect();
        }

        private void Global_OnReceivedGlobalSettings(object sender, ReceivedGlobalSettingsPayload payload)
        {
            if (payload?.Settings != null)
            {
                global = payload.Settings.ToObject<TwitchGlobalSettings>();
            }
        }

        #endregion

        #region Pubsub Callbacks

        private void Pubsub_OnListenResponse(object sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Listening to topic: {e.Topic} Status: {e.Successful}");
        }

        private void Pubsub_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            RegisterEventsToListenTo();
        }
        private void Pubsub_OnStreamDown(object sender, TwitchLib.PubSub.Events.OnStreamDownArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Stream Down Received");
        }

        private void Pubsub_OnStreamUp(object sender, TwitchLib.PubSub.Events.OnStreamUpArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Stream Up Received");
        }

        private void Instance_TokensChanged(object sender, TwitchTokenEventArgs e)
        {
            if (TwitchTokenManager.Instance.TokenExists)
            {
                if (connectCalled) // Already connected, try registering for pubsub events
                {
                    connectCalled = false; // Reregister if tokens changed
                    RegisterEventsToListenTo();
                }
                else // Not connected - start by connecting to pubsub
                {
                    ConnectPubSub();
                }
            }
        }

        private async void Pubsub_OnChannelPoints(object sender, TwitchLib.PubSub.Events.OnChannelPointsRedeemedArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{e.DisplayName} redeemed {e.Title} for {e.PointsUsed} points. {(String.IsNullOrEmpty(e.UserInput) ? "" : "Message: " + e.UserInput)}");
            
            // Check if channel is live
            var channelInfo = await TwitchChannelInfoManager.Instance.GetChannelInfo(channelName);
            if (channelInfo != null && !channelInfo.IsLive)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Not raising Channel Points because channel isn't live");
                return;
            }

            // Send Chat Message
            if (!String.IsNullOrEmpty(global.PointsChatMessage))
            {
                TwitchChat.Instance.SendMessage(global.PointsChatMessage.Replace(@"\n", "\n").Replace("{USERNAME}", e.Username).Replace("{DISPLAYNAME}", e.DisplayName).Replace("{TITLE}", e.Title).Replace("{POINTS}", e.PointsUsed.ToString()).Replace("{MESSAGE}", e.UserInput));
            }

            if (!String.IsNullOrEmpty(global.PointsFlashMessage))
            {
                TwitchChat.Instance.RaisePageAlert(global.PointsFlashMessage.Replace("{USERNAME}", e.Username).Replace("{DISPLAYNAME}", e.DisplayName).Replace("{TITLE}", e.Title).Replace("{POINTS}", e.PointsUsed.ToString()).Replace("{MESSAGE}", e.UserInput), global.PointsFlashColor);
            }
        }

        private async void Pubsub_OnChannelSubscription(object sender, TwitchLib.PubSub.Events.OnChannelSubscriptionArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Sub: {e.Subscription.DisplayName} {(e.Subscription.UserId != e.Subscription.RecipientId ? "gifted to " + e.Subscription.RecipientName : "")}");
            // Check if channel is live
            var channelInfo = await TwitchChannelInfoManager.Instance.GetChannelInfo(channelName);
            if (channelInfo != null && !channelInfo.IsLive)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Not raising On Sub because channel isn't live");
                return;
            }

            // Send Chat Message
            if (!String.IsNullOrEmpty(global.SubChatMessage))
            {
                TwitchChat.Instance.SendMessage(global.SubChatMessage.Replace(@"\n", "\n").Replace("{USERNAME}", e.Subscription.Username).Replace("{DISPLAYNAME}", e.Subscription.DisplayName).Replace("{RecipientName}", e.Subscription.RecipientName).Replace("{MESSAGE}", e.Subscription.SubMessage.Message).Replace("{MONTHS}", e.Subscription.Months.ToString()));
            }

            if (!String.IsNullOrEmpty(global.SubFlashMessage))
            {
                TwitchChat.Instance.RaisePageAlert(global.SubFlashMessage.Replace("{USERNAME}", e.Subscription.Username).Replace("{DISPLAYNAME}", e.Subscription.DisplayName).Replace("{RecipientName}", e.Subscription.RecipientName).Replace("{MESSAGE}", e.Subscription.SubMessage.Message).Replace("{MONTHS}", e.Subscription.Months.ToString()), global.SubFlashColor);
            }
        }

        private async void Pubsub_OnBitsReceived(object sender, TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{e.Username} cheered {e.BitsUsed}/{e.TotalBitsUsed} bits. Message: {e.ChatMessage}");
            
            // Check if channel is live
            var channelInfo = await TwitchChannelInfoManager.Instance.GetChannelInfo(channelName);
            if (channelInfo != null && !channelInfo.IsLive)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Not raising Bits Received because channel isn't live");
                return;
            }

            // Send Chat Message
            if (!String.IsNullOrEmpty(global.BitsChatMessage))
            {
                TwitchChat.Instance.SendMessage(global.BitsChatMessage.Replace(@"\n", "\n").Replace("{USERNAME}", e.Username).Replace("{DISPLAYNAME}", e.Username).Replace("{BITS}", e.BitsUsed.ToString()).Replace("{MESSAGE}", e.ChatMessage).Replace("{TOTALBITS}", e.TotalBitsUsed.ToString()));
            }

            if (!String.IsNullOrEmpty(global.BitsFlashMessage))
            {
                TwitchChat.Instance.RaisePageAlert(global.BitsFlashMessage.Replace("{USERNAME}", e.Username).Replace("{DISPLAYNAME}", e.Username).Replace("{BITS}", e.BitsUsed.ToString()).Replace("{MESSAGE}", e.ChatMessage).Replace("{TOTALBITS}", e.TotalBitsUsed.ToString()), global.BitsFlashColor);
            }

        }

        private async void Pubsub_OnFollow(object sender, TwitchLib.PubSub.Events.OnFollowArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"New Follower: {e.DisplayName}");

            string follower = e.DisplayName.ToLowerInvariant();
            if (pastFollowers.Contains(follower))
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Not raising On Follow because {e.DisplayName} already followed recently");
                return;
            }
            pastFollowers.Add(follower);

            // Check if channel is live
            var channelInfo = await TwitchChannelInfoManager.Instance.GetChannelInfo(channelName);
            if (channelInfo != null && !channelInfo.IsLive)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Not raising On Follow because channel isn't live");
                return;
            }

            // Send Chat Message
            if (!String.IsNullOrEmpty(global.FollowChatMessage))
            {
                TwitchChat.Instance.SendMessage(global.FollowChatMessage.Replace(@"\n", "\n").Replace("{USERNAME}", e.Username).Replace("{DISPLAYNAME}", e.DisplayName));
            }

            if (!String.IsNullOrEmpty(global.FollowFlashMessage))
            {
                TwitchChat.Instance.RaisePageAlert(global.FollowFlashMessage.Replace("{USERNAME}", e.Username).Replace("{DISPLAYNAME}", e.DisplayName), global.FollowFlashColor);
            }
        }

        #endregion
    }
}
