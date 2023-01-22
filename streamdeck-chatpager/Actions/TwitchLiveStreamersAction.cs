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
    // Subscriber: nubby_ninja
    //---------------------------------------------------    
    [PluginActionId("com.barraider.twitchtools.livestreamers")]
    public class TwitchLiveStreamersAction : ActionBase
    {
        protected class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    TokenExists = false,
                    LongPressAction = TwitchLiveStreamersLongPressAction.Raid,
                    FilteredUsers = String.Empty,
                    LiveStreamPreview = true,
                    LiveGameIcon = false,
                    LiveUserIcon = false,
                };
                return instance;
            }

            [JsonProperty(PropertyName = "longPressAction")]
            public TwitchLiveStreamersLongPressAction LongPressAction { get; set; }

            [JsonProperty(PropertyName = "filteredUsers")]
            public string FilteredUsers { get; set; }

            [JsonProperty(PropertyName = "liveStreamPreview")]
            public bool LiveStreamPreview { get; set; }

            [JsonProperty(PropertyName = "liveGameIcon")]
            public bool LiveGameIcon { get; set; }

            [JsonProperty(PropertyName = "liveUserIcon")]
            public bool LiveUserIcon { get; set; }
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

        public TwitchLiveStreamersAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            var streamers = await TwitchChannelInfoManager.Instance.GetActiveStreamers();
            if (streamers != null)
            {
                if (!String.IsNullOrWhiteSpace(Settings.FilteredUsers))
                {
                    var filteredUsers = Settings.FilteredUsers?.Replace("\r\n", "\n").ToLowerInvariant().Split('\n').ToList();
                    streamers = streamers.Where(s => !filteredUsers.Contains(s.UserName.ToLowerInvariant())).ToArray();
                }
                AlertManager.Instance.Initialize(Connection);


                ChannelDisplayImage cdi = ChannelDisplayExtensionMethods.FromSettings(Settings.LiveStreamPreview, Settings.LiveGameIcon, Settings.LiveUserIcon);
                AlertManager.Instance.ShowActiveStreamers(new TwitchLiveStreamersDisplaySettings(streamers.Reverse().ToArray(), Settings.LongPressAction, cdi));
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Key Pressed but GetActiveStreamers returned null");
                await Connection.ShowAlert();
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

            if (TwitchTokenManager.Instance.TokenExists)
            {
                var streamers = await TwitchChannelInfoManager.Instance.GetActiveStreamers();
                await Connection.SetTitleAsync($"🔴 {streamers?.Length} live");
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

        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        private void InitializeSettings()
        {
            // Backwards competability
            if (!Settings.LiveStreamPreview && !Settings.LiveGameIcon && !Settings.LiveUserIcon)
            {
                Settings.LiveStreamPreview = true;
            }

            if (Settings.LongPressAction != TwitchLiveStreamersLongPressAction.Raid)
            {
                Settings.LongPressAction = TwitchLiveStreamersLongPressAction.Raid;
            }

            SaveSettings();
        }

        #endregion
    }
}
