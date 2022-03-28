using BarRaider.SdTools;
using ChatPager.Twitch;
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
    // 500 Bits: Nachtmeister666
    //---------------------------------------------------

    [PluginActionId("com.barraider.twitchtools.cliptochat")]
    public class TwitchClipAction : ActionBase
    {
        protected class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    TokenExists = false,
                    Channel = String.Empty,
                    ChatMessage = String.Empty
                };
                return instance;
            }

            [JsonProperty(PropertyName = "channel")]
            public string Channel { get; set; }

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

        public TwitchClipAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            SaveSettings();
        }

        public override void Dispose() { }

        public override async void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
            if (!TwitchTokenManager.Instance.TokenExists)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "TwitchClipAction called without a valid token");
                await Connection.ShowAlert();
                return;
            }

            if (await CreateClip())
            {
                await Connection.ShowOk();
            }
            else
            {
                await Connection.ShowAlert();
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
            SaveSettings();
        }

        #endregion

        #region Private Methods

        public async Task<bool> CreateClip()
        {
            try
            {
                using (TwitchComm comm = new TwitchComm())
                {
                    string channel = TwitchTokenManager.Instance.User.UserName;
                    if (!String.IsNullOrEmpty(Settings.Channel))
                    {
                        channel = Settings.Channel;
                    }

                    var clip = await comm.CreateClip(channel);
                    if (clip != null)
                    {                        
                        string clipUrl = clip.EditURL.Replace("/edit", "");
                        TwitchChat.Instance.SendMessage(channel, $"{Settings.ChatMessage} {clipUrl}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Could not create Twitch Clip: {ex}");
            }
            return false;
        }

        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        #endregion
    }
}
