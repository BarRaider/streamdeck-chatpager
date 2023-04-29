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
                    BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, $"Pubsub internal log: {message.Replace("\r\n","")} {exception}");
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
            TwitchChat.Instance.OnRaidReceived += Instance_OnRaidReceived;
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
            pubsub.OnRewardRedeemed += Pubsub_OnRewardRedeemed;
            pubsub.OnFollow += Pubsub_OnFollow;
            pubsub.OnHost += Pubsub_OnHost;
            pubsub.OnRaidUpdateV2 += Pubsub_OnRaidUpdateV2;
            pubsub.OnRaidGo += Pubsub_OnRaidGo;

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
            pubsub.OnRewardRedeemed -= Pubsub_OnRewardRedeemed;
            pubsub.OnFollow -= Pubsub_OnFollow;
            pubsub.OnHost -= Pubsub_OnHost;
            pubsub.OnRaidUpdateV2 -= Pubsub_OnRaidUpdateV2;
            pubsub.OnRaidGo -= Pubsub_OnRaidGo;

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
            pubsub.ListenToRaid(channelName);
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
                bool wasEnabled = global?.PubsubNotifications ?? false;
                global = payload.Settings.ToObject<TwitchGlobalSettings>();

                {
                    Initialize();
                }
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

        private async void Pubsub_OnRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnRewardRedeemedArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{e.DisplayName} redeemed {e.RewardTitle} for {e.RewardCost} points. {(String.IsNullOrEmpty(e.Message) ? "" : "Message: " + e.Message)}");
            
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
                TwitchChat.Instance.SendMessage(global.PointsChatMessage.Replace(@"\n", "\n").Replace("{USERNAME}", e.Login).Replace("{DISPLAYNAME}", e.DisplayName).Replace("{TITLE}", e.RewardTitle).Replace("{POINTS}", e.RewardCost.ToString()).Replace("{MESSAGE}", e.Message));
            }

            if (!String.IsNullOrEmpty(global.PointsFlashMessage))
            {
                TwitchChat.Instance.RaisePageAlert(e.DisplayName, global.PointsFlashMessage.Replace("{USERNAME}", e.Login).Replace("{DISPLAYNAME}", e.DisplayName).Replace("{TITLE}", e.RewardTitle).Replace("{POINTS}", e.RewardCost.ToString()).Replace("{MESSAGE}", e.Message), global.PointsFlashColor);
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
                TwitchChat.Instance.RaisePageAlert(e.Subscription.DisplayName, global.SubFlashMessage.Replace("{USERNAME}", e.Subscription.Username).Replace("{DISPLAYNAME}", e.Subscription.DisplayName).Replace("{RecipientName}", e.Subscription.RecipientName).Replace("{MESSAGE}", e.Subscription.SubMessage.Message).Replace("{MONTHS}", e.Subscription.Months.ToString()), global.SubFlashColor);
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
                TwitchChat.Instance.RaisePageAlert(e.Username, global.BitsFlashMessage.Replace("{USERNAME}", e.Username).Replace("{DISPLAYNAME}", e.Username).Replace("{BITS}", e.BitsUsed.ToString()).Replace("{MESSAGE}", e.ChatMessage).Replace("{TOTALBITS}", e.TotalBitsUsed.ToString()), global.BitsFlashColor);
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
                TwitchChat.Instance.RaisePageAlert(e.DisplayName, global.FollowFlashMessage.Replace("{USERNAME}", e.Username).Replace("{DISPLAYNAME}", e.DisplayName), global.FollowFlashColor);
            }
        }

        private async void Instance_OnRaidReceived(object sender, TwitchLib.Client.Events.OnRaidNotificationArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{e.RaidNotification.DisplayName} raided channel {e.RaidNotification.RoomId} with {e.RaidNotification.MsgParamViewerCount} viewers");

            // Check if channel is live
            var channelInfo = await TwitchChannelInfoManager.Instance.GetChannelInfo(channelName);
            if (channelInfo != null && !channelInfo.IsLive)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Not raising Raid event because channel isn't live");
                return;
            }

            if (!global.PubsubNotifications)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Not raising Raid event because pubsub notifications are disabled");
                return;
            }

            // Send Chat Message
            if (!String.IsNullOrEmpty(global.RaidChatMessage))
            {
                TwitchChat.Instance.SendMessage(global.RaidChatMessage.Replace(@"\n", "\n").Replace("{USERNAME}", e.RaidNotification.Login).Replace("{DISPLAYNAME}", e.RaidNotification.DisplayName).Replace("{VIEWERS}", e.RaidNotification.MsgParamViewerCount));
            }

            if (!String.IsNullOrEmpty(global.RaidFlashMessage))
            {
                TwitchChat.Instance.RaisePageAlert(e.RaidNotification.DisplayName, global.RaidFlashMessage.Replace("{USERNAME}", e.RaidNotification.Login).Replace("{DISPLAYNAME}", e.RaidNotification.DisplayName).Replace("{VIEWERS}", e.RaidNotification.MsgParamViewerCount), global.RaidFlashColor);
            }
        }

        private void Pubsub_OnRaidGo(object sender, TwitchLib.PubSub.Events.OnRaidGoArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"*** OnRaidGo PubSub Target: {e.TargetDisplayName} Source: {e.ChannelId}");
        }

        private void Pubsub_OnRaidUpdateV2(object sender, TwitchLib.PubSub.Events.OnRaidUpdateV2Args e)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"*** OnRaidUpdateV2 PubSub Target: {e.TargetDisplayName} Source: {e.ChannelId}");
        }

        private void Pubsub_OnHost(object sender, TwitchLib.PubSub.Events.OnHostArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"*** OnHost PubSub Host: {e.HostedChannel} Source: {e.ChannelId}");
        }

        #endregion
    }
}
