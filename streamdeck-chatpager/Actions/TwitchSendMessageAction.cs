using BarRaider.SdTools;
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

    [PluginActionId("com.barraider.twitchtools.sendmessage")]
    public class TwitchSendMessageAction : ActionBase
    {
        protected class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    TokenExists = false,
                    ChatMessage = String.Empty,
                    Channel = String.Empty,
                    LoadFromFile = false,
                    MessageFile = String.Empty,
                    IsAnnouncement = false
                };
                return instance;
            }

            [JsonProperty(PropertyName = "channel")]
            public string Channel { get; set; }

            [JsonProperty(PropertyName = "chatMessage")]
            public string ChatMessage { get; set; }

            [JsonProperty(PropertyName = "loadFromFile")]
            public bool LoadFromFile { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "messageFile")]
            public string MessageFile { get; set; }

            [JsonProperty(PropertyName = "announcement")]
            public bool IsAnnouncement { get; set; }

            
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

        public TwitchSendMessageAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} called without a valid token");
                await Connection.ShowAlert();
                return;
            }

            string channel = TwitchTokenManager.Instance.User?.UserName;
            if (!String.IsNullOrEmpty(Settings.Channel))
            {
                channel = Settings.Channel;
            }

            string message = Settings.ChatMessage;
            if (Settings.LoadFromFile)
            {
                if (String.IsNullOrEmpty(Settings.MessageFile) || !File.Exists(Settings.MessageFile))
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} called with LoadFromFile but invalid file name {Settings.MessageFile}");
                    await Connection.ShowAlert();
                    return;
                }

                message = File.ReadAllText(Settings.MessageFile);
            }

            if (String.IsNullOrEmpty(message))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} called but no message is set");
                await Connection.ShowAlert();
                return;
            }

            if (Settings.IsAnnouncement)
            {
                using (TwitchComm tc = new TwitchComm())
                {
                    if (await tc.SendAnnouncement(channel, message))
                    {
                        await Connection.ShowOk();
                    }
                    else
                    {
                        await Connection.ShowAlert();
                    }
                }
            }
            else
            {
                TwitchChat.Instance.SendMessage(channel, message);
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

        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        #endregion
    }
}
