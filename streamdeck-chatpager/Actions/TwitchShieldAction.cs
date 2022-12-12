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
using System.Threading.Tasks;

namespace ChatPager.Actions
{
    //---------------------------------------------------
    //          BarRaider's Hall Of Fame
    // Subscriber: nubby_ninja
    // 200 Bits: Nachtmeister666
    // Subscriber: icessassin
    // Subscriber: Vedeksu
    // Subscriber: Vedeksu x2 !!!!!!!
    // Subscriber: Pr0xity
    //---------------------------------------------------

    [PluginActionId("com.barraider.twitchtools.shield")]
    public class TwitchShieldAction : ActionBase
    {
        protected class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    TokenExists = false,
                    ChannelName = String.Empty,
                    HideChannelName = false,
                };
                return instance;
            }

            [JsonProperty(PropertyName = "channelName")]
            public string ChannelName { get; set; }

            [JsonProperty(PropertyName = "hideChannelName")]
            public bool HideChannelName { get; set; }
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

        private const int STATUS_REFRESH_COOLDOWN_MS = 5000;
        private const string SHIELD_ENABLED_IMAGE_FILE = @"images\shieldOn.png";


        private DateTime lastStatusUpdate = DateTime.MinValue;
        private bool isShieldEnabled = false;
        private Image prefetchedShieldEnabledImage = null;
        TwitchComm comm = new TwitchComm();

        #endregion

        #region Public Methods

        public TwitchShieldAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            SaveSettings();
        }

        public override void Dispose() { }

        public override async void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");

            if (String.IsNullOrEmpty(Settings.ChannelName))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} KeyPressed but channel name is null!");
                await Connection.ShowAlert();
                return;
            }

            if (await comm.SetShieldStatus(Settings.ChannelName, !isShieldEnabled))
            {
                await Connection.ShowOk();
            }
            else
            { 
                await Connection.ShowAlert();
            }
            
            lastStatusUpdate = DateTime.MinValue;
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

            if (!String.IsNullOrEmpty(Settings.ChannelName) && TwitchTokenManager.Instance.TokenExists && ((DateTime.Now - lastStatusUpdate).TotalMilliseconds >= STATUS_REFRESH_COOLDOWN_MS))
            {
                await RefreshShieldStatus();
            }
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            string channelName = Settings.ChannelName;
            Tools.AutoPopulateSettings(Settings, payload.Settings);

            if (channelName != Settings.ChannelName)
            {
                lastStatusUpdate = DateTime.MinValue;
            }
            InitializeSettings();
            SaveSettings();
        }

        #endregion

        #region Private Methods

        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        private async Task RefreshShieldStatus()
        {
            var shieldStatus = await comm.IsShieldEnabled(Settings.ChannelName);
            lastStatusUpdate = DateTime.Now;
            if (shieldStatus == null || !shieldStatus.HasValue)
            {
                isShieldEnabled = false;
            }
            else
            {
                isShieldEnabled = shieldStatus.Value;
            }

            if (isShieldEnabled)
            {
                await Connection.SetImageAsync(GetShieldEnabledImage());
            }
            else
            {
                await Connection.SetImageAsync((String)null);
            }
        }

     
        private void InitializeSettings()
        {
        }

        private Image GetShieldEnabledImage()
        {
            if (prefetchedShieldEnabledImage == null)
            {
                if (File.Exists(SHIELD_ENABLED_IMAGE_FILE))
                {
                    prefetchedShieldEnabledImage = Image.FromFile(SHIELD_ENABLED_IMAGE_FILE);
                }
            }

            return prefetchedShieldEnabledImage;
        }

        #endregion
    }
}
