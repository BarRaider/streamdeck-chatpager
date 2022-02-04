using BarRaider.SdTools;
using ChatPager.Backend;
using ChatPager.Twitch;
using ChatPager.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Actions
{

    //---------------------------------------------------
    //          BarRaider's Hall Of Fame
    // 100 Bits: Vedeksu
    //---------------------------------------------------

    [PluginActionId("com.barraider.twitchtools.shoutout")]
    public class TwitchShoutoutAction : ActionBase
    {
        public enum UsersDisplay
        {
            ActiveChatters,
            AllViewers
        }
        protected class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    TokenExists = false,
                    ChatMessage = DEFAULT_CHAT_MESSAGE,
                    Channel = String.Empty,
                    UsersDisplay = UsersDisplay.ActiveChatters,
                    DontLoadImages = false,
                    Suffix = String.Empty
                };
                return instance;
            }

            [JsonProperty(PropertyName = "chatMessage")]
            public string ChatMessage { get; set; }

            [JsonProperty(PropertyName = "channel")]
            public string Channel { get; set; }

            [JsonProperty(PropertyName = "usersDisplay")]
            public UsersDisplay UsersDisplay { get; set; }

            [JsonProperty(PropertyName = "dontLoadImages")]
            public bool DontLoadImages { get; set; }

            [JsonProperty(PropertyName = "suffix")]
            public string Suffix { get; set; }
        }

        protected PluginSettings Settings
        {
            get
            {
                var result = settings as PluginSettings;
                if (result == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot convert PluginSettingsBase to PluginSettings");
                }
                return result;
            }
            set
            {
                settings = value;
            }
        }

        #region Private Members

        private const string DEFAULT_CHAT_MESSAGE = "!so {USERNAME}";

        #endregion

        #region Public Methods

        public TwitchShoutoutAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.Settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.Settings = payload.Settings.ToObject<PluginSettings>();
            }

            Settings.TokenExists = TwitchTokenManager.Instance.TokenExists;
            TwitchChat.Instance.Initialize();
            InitializeSettings();
        }

        public override void Dispose() { }

        public override async void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
            if (!TwitchTokenManager.Instance.TokenExists)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} called without a valid token");
                await Connection.ShowAlert();
                return;
            }

            if (String.IsNullOrEmpty(Settings.ChatMessage))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} called without a valid message/action");
                await Connection.ShowAlert();
                return;
            }

            string[] userNames;
            if (Settings.UsersDisplay == UsersDisplay.ActiveChatters)
            {
                var chatters = TwitchChat.Instance.GetLastChatters();
                if (chatters == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "GetLastChatters returned null");
                    await Connection.ShowAlert();
                    return;
                }

                Logger.Instance.LogMessage(TracingLevel.INFO, "Shoutout loading active chatters");
                userNames = chatters.ToArray();
            }
            else // Show all viewers
            {
                string channel = Settings.Channel;
                if (String.IsNullOrEmpty(channel))
                {
                    channel = TwitchTokenManager.Instance.User.UserName;
                }
                var viewers = await TwitchChannelInfoManager.Instance.GetChannelViewers(channel);
                if (viewers == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "GetChannelViewers returned null");
                    await Connection.ShowAlert();
                    return;
                }
                Logger.Instance.LogMessage(TracingLevel.INFO, "Shoutout loading all viewers");
                userNames = viewers.AllViewers.OrderBy(u => u.ToLowerInvariant()).ToArray();
            }           

            // We have a list of usernames, get some more details on them so we can display their image on the StreamDeck
            List<ChatMessageKey> chatMessages = new List<ChatMessageKey>();
            foreach (string username in userNames)
            {
                TwitchUserInfo userInfo = null;
                if (!Settings.DontLoadImages)
                {
                    userInfo = await TwitchUserInfoManager.Instance.GetUserInfo(username);
                }
                chatMessages.Add(new ChatMessageKey(userInfo?.Name ?? username, userInfo?.ProfileImageUrl, Settings.ChatMessage.Replace("{USERNAME}", $"{userInfo?.Name ?? username}").Replace("{SUFFIX}", Settings.Suffix)));
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPress returned {chatMessages?.Count} chat messages");

            // Show the active chatters on the StreamDeck
            if (chatMessages != null && chatMessages.Count > 0)
            {
                AlertManager.Instance.Initialize(Connection);
                AlertManager.Instance.ShowChatMessages(chatMessages.ToArray(), Settings.Channel);
            }
            else
            {
                await Connection.ShowOk();
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick()
        {
            baseHandledOnTick = false;
            base.OnTick();

            if (baseHandledOnTick)
            {
                return;
            }

        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) 
        {
            Tools.AutoPopulateSettings(Settings, payload.Settings);
            InitializeSettings();
        }

        #endregion

        #region Private Methods

        protected virtual void InitializeSettings()
        {
            if (!string.IsNullOrEmpty(Settings.Channel) && Settings.UsersDisplay == UsersDisplay.ActiveChatters)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"Shoutout does not support 'Active Chatters' mode when 'Channel' property is set");
                Settings.UsersDisplay = UsersDisplay.AllViewers;
            }
            SaveSettings();
        }

        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        #endregion
    }
}
