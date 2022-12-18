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
    public class TwitchShoutoutAction : UserSelectionActionBase
    {
        protected class PluginSettings : UserSelectionPluginSettings
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
                };
                return instance;
            }

            [JsonProperty(PropertyName = "chatMessage")]
            public string ChatMessage { get; set; }
        }

        protected PluginSettings Settings
        {
            get
            {
                var result = settings as PluginSettings;
                if (result == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} Cannot convert PluginSettingsBase to PluginSettings");
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
            InitializeSettings();
        }

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

            string[] userNames = await GetUsersList();
            if (userNames == null)
            {
                await Connection.ShowAlert();
                return;
            }

            // We have a list of usernames, get some more details on them so we can display their image on the StreamDeck
            List<UserSelectionEventSettings> chatSettings = new List<UserSelectionEventSettings>();
            foreach (string username in userNames)
            {
                TwitchUserInfo userInfo = null;
                if (!Settings.DontLoadImages)
                {
                    userInfo = await TwitchUserInfoManager.Instance.GetUserInfo(username);
                }
                chatSettings.Add(new UserSelectionEventSettings(UserSelectionEventType.ChatMessage, userInfo?.Name ?? username, userInfo?.ProfileImageUrl, userInfo?.UserId ?? null, Settings.ChatMessage.Replace("{USERNAME}", $"{userInfo?.Name ?? username}")));
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPress returned {chatSettings?.Count} users");

            // Show the active chatters on the StreamDeck
            if (chatSettings != null && chatSettings.Count > 0)
            {
                AlertManager.Instance.Initialize(Connection);
                AlertManager.Instance.ShowUserSelectionEvent(chatSettings.ToArray(), Settings.Channel);
            }
            else
            {
                await Connection.ShowOk();
            }
        }

        public override void OnTick()
        {
            baseHandledOnTick = false;
            base.OnTick();

            if (baseHandledOnTick)
            {
                return;
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) 
        {
            Tools.AutoPopulateSettings(Settings, payload.Settings);
            InitializeSettings();
            SaveSettings();
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
