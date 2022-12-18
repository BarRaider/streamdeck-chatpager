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
    // Subscriber: KreatureOfHaviQ
    // Quote of the day: "Bots have feelings too..." 1/29 - BarRaider
    //---------------------------------------------------

    [PluginActionId("com.barraider.twitchtools.channelviewers")]
    public class TwitchChannelViewersAction : ActionBase
    {
        protected class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    TokenExists = false,
                    ChannelName = string.Empty,
                    DontLoadImages = false
                };
                return instance;
            }

            [JsonProperty(PropertyName = "channelName")]
            public string ChannelName { get; set; }

            [JsonProperty(PropertyName = "dontLoadImages")]
            public bool DontLoadImages { get; set; }
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

        #endregion

        #region Public Methods

        public TwitchChannelViewersAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            SaveSettings();
        }

        public override void Dispose() { }

        public override async void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
            var viewers = await TwitchChannelInfoManager.Instance.GetChannelViewers(Settings.ChannelName);
            if (viewers == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} GetChannelViewers returned null!");
                await Connection.ShowAlert();
                return;
            }

            // We have a list of usernames, get some more details on them so we can display their image on the StreamDeck
            List<UserSelectionEventSettings> chatSettings = new List<UserSelectionEventSettings>();
            foreach (string username in viewers?.AllViewers)
            {
                TwitchUserInfo userInfo = null;
                if (!Settings.DontLoadImages)
                {
                    userInfo = await TwitchUserInfoManager.Instance.GetUserInfo(username);
                }
                chatSettings.Add(new UserSelectionEventSettings(UserSelectionEventType.ChatMessage, userInfo?.Name ?? username, userInfo?.ProfileImageUrl));
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPress returned {chatSettings?.Count} viewers");

            // Show the active chatters on the StreamDeck
            if (chatSettings != null && chatSettings.Count > 0)
            {
                AlertManager.Instance.Initialize(Connection);
                AlertManager.Instance.ShowUserSelectionEvent(chatSettings.OrderBy(c => c.KeyTitle.ToLowerInvariant()).ToArray(), null);
            }
            else
            {
                await Connection.ShowOk();
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public async override void OnTick()
        {
            baseHandledOnTick = false;
            base.OnTick();

            if (baseHandledOnTick)
            {
                return;
            }

            if (TwitchTokenManager.Instance.TokenExists && !String.IsNullOrEmpty(Settings.ChannelName))
            {
                var viewers = await TwitchChannelInfoManager.Instance.GetChannelViewers(Settings.ChannelName);
                await Connection.SetTitleAsync($"👀 {viewers?.TotalViewers}");
            }
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(Settings, payload.Settings);
            SaveSettings();
        }

        #endregion

        #region Private Methods

        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        #endregion
    }
}
