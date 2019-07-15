using BarRaider.SdTools;
using ChatPager.Twitch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace ChatPager
{
    [PluginActionId("com.barraider.twitchpager")]
    public class TwitchPager : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.TokenExists = false;
                instance.PageCooldown = 30;
                instance.AllowedPagers = String.Empty;
                instance.ChatMessage = String.Empty;
                instance.DashboardOnClick = true;
                instance.FullScreenAlert = true;
                instance.TwoLettersPerKey = false;
                instance.AlertColor = "#FF0000";
                return instance;
            }

            [JsonProperty(PropertyName = "tokenExists")]
            public bool TokenExists { get; set; }

            [JsonProperty(PropertyName = "pageCooldown")]
            public int PageCooldown { get; set; }

            [JsonProperty(PropertyName = "allowedPagers")]
            public string AllowedPagers { get; set; }

            [JsonProperty(PropertyName = "chatMessage")]
            public string ChatMessage { get; set; }

            [JsonProperty(PropertyName = "dashboardOnClick")]
            public bool DashboardOnClick { get; set; }

            [JsonProperty(PropertyName = "fullScreenAlert")]
            public bool FullScreenAlert { get; set; }

            [JsonProperty(PropertyName = "twoLettersPerKey")]
            public bool TwoLettersPerKey { get; set; }

            [JsonProperty(PropertyName = "alertColor")] 
            public string AlertColor { get; set; }
        }

        #region Private members

        private const string BACKGROUND_COLOR = "#8560db";

        private PluginSettings settings;
        private bool isPaging = false;
        private System.Timers.Timer tmrPage = new System.Timers.Timer();
        private int alertStage = 0;
        private TwitchStreamInfo streamInfo;
        private string pageMessage = null;
        private bool fullScreenAlertTriggered = false;
        private StreamDeckDeviceType deviceType;
        private TwitchGlobalSettings global = null;

        #endregion

        public TwitchPager(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
            TwitchStreamInfoManager.Instance.TwitchStreamInfoChanged += Instance_TwitchStreamInfoChanged;
            TwitchTokenManager.Instance.TokenStatusChanged += Instance_TokenStatusChanged;
            Connection.StreamDeckConnection.OnSendToPlugin += StreamDeckConnection_OnSendToPlugin;
            TwitchChat.Instance.PageRaised += Chat_PageRaised;

            this.settings.ChatMessage = TwitchChat.Instance.ChatMessage;
            settings.TokenExists = TwitchTokenManager.Instance.TokenExists;
            ResetChat();
            deviceType = Connection.DeviceInfo().Type;
            
            tmrPage.Interval = 200;
            tmrPage.Elapsed += TmrPage_Elapsed;
            SaveSettings();
            Connection.GetGlobalSettingsAsync();
        }

        #region PluginBase Implementation

        public override void Dispose()
        {
            TwitchStreamInfoManager.Instance.TwitchStreamInfoChanged -= Instance_TwitchStreamInfoChanged;
            TwitchTokenManager.Instance.TokenStatusChanged -= Instance_TokenStatusChanged;
            Connection.StreamDeckConnection.OnSendToPlugin -= StreamDeckConnection_OnSendToPlugin;
            TwitchChat.Instance.PageRaised -= Chat_PageRaised;
            tmrPage.Stop();
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor Called");
        }

        public override void KeyPressed(KeyPayload payload)
        {
            //Chat_PageRaised(this, new PageRaisedEventArgs("This is a test"));
            //RaiseFullScreenAlert();
            if (isPaging)
            {
                isPaging = false;
                pageMessage = null;
            }
            else
            {
                if (!TwitchStreamInfoManager.Instance.IsLive || streamInfo == null)
                {
                    TwitchStreamInfoManager.Instance.ForceStreamInfoRefresh();
                }
                else if (TwitchTokenManager.Instance.User != null && settings.DashboardOnClick)
                {
                    System.Diagnostics.Process.Start(String.Format("https://www.twitch.tv/{0}/dashboard/live", TwitchTokenManager.Instance.User.UserName));
                }
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick()
        {
            if (isPaging && !tmrPage.Enabled && !settings.FullScreenAlert)
            {
                alertStage = 0;
                tmrPage.Start();
            }
            else if (isPaging && settings.FullScreenAlert && !fullScreenAlertTriggered)
            {
                RaiseFullScreenAlert();
            }
            else if (!isPaging)
            {
                tmrPage.Stop();
                DrawStreamData();
            }
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            // Global Settings exist
            if (payload?.Settings != null && payload.Settings.Count > 0)
            {
                global = payload.Settings.ToObject<TwitchGlobalSettings>();
                TwitchChat.Instance.SetChatMessage(global.ChatMessage);
                settings.ChatMessage = TwitchChat.Instance.ChatMessage;
                settings.TwoLettersPerKey = global.TwoLettersPerKey;
                settings.AlertColor = global.InitialAlertColor;
                SaveSettings();
            }
            else // Global settings do not exist
            {
                global = new TwitchGlobalSettings();
                global.ChatMessage = TwitchChat.Instance.ChatMessage;
                global.TwoLettersPerKey = settings.TwoLettersPerKey;
                global.InitialAlertColor = settings.AlertColor;
                Connection.SetGlobalSettingsAsync(JObject.FromObject(global));
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            // Save original values
            string oldChatMessage = settings.ChatMessage;
            bool twoLettersPerKey = settings.TwoLettersPerKey;
            string alertColor = settings.AlertColor;

            // Populate new values
            Tools.AutoPopulateSettings(settings, payload.Settings);
            ResetChat();
            if (oldChatMessage != settings.ChatMessage || twoLettersPerKey != settings.TwoLettersPerKey || alertColor != settings.AlertColor)
            {
                SetGlobalSettings();
            }
        }

        #endregion

        #region Private Members

        private void SetGlobalSettings()
        {
            if (global == null)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "SetGlobalSettings called while Global Settings are null");
                global = new TwitchGlobalSettings();
            }

            global.ChatMessage = settings.ChatMessage;
            global.TwoLettersPerKey = settings.TwoLettersPerKey;
            global.InitialAlertColor = settings.AlertColor;
            Connection.SetGlobalSettingsAsync(JObject.FromObject(global));
        }

        private void TmrPage_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Bitmap img = Tools.GenerateKeyImage(deviceType, out Graphics graphics);
            int height = Tools.GetKeyDefaultHeight(deviceType);
            int width = Tools.GetKeyDefaultWidth(deviceType);

            // Background
            
            var bgBrush = new SolidBrush(Helpers.GenerateStageColor(settings.AlertColor, alertStage, Helpers.TOTAL_ALERT_STAGES));
            graphics.FillRectangle(bgBrush, 0, 0, width, height);

            if (String.IsNullOrEmpty(pageMessage))
            {
                Connection.SetImageAsync(img);
            }
            else
            {
                var font = new Font("Verdana", 11, FontStyle.Bold);
                var fgBrush = Brushes.White;
                SizeF stringSize = graphics.MeasureString(pageMessage, font);
                float stringPos = 0;
                float stringHeight = Math.Abs((height - stringSize.Height)) / 2;
                if (stringSize.Width < width)
                {
                    stringPos = Math.Abs((width - stringSize.Width)) / 2;
                }
                else // Move to multi line
                {
                    stringHeight = 0;
                    pageMessage = pageMessage.Replace(" ", "\n");
                }
                graphics.DrawString(pageMessage, font, fgBrush, new PointF(stringPos, stringHeight));
                Connection.SetImageAsync(img);
            }
            alertStage = (alertStage + 1) % Helpers.TOTAL_ALERT_STAGES;
        }

        private async void DrawStreamData()
        {
            try
            {
                if (!settings.TokenExists)
                {
                    await Connection.SetImageAsync(Properties.Settings.Default.TwitchNoToken).ConfigureAwait(false);
                    return;
                }

                if (!TwitchStreamInfoManager.Instance.IsLive || streamInfo == null)
                {
                    await Connection.SetImageAsync(Properties.Settings.Default.TwitchNotLive).ConfigureAwait(false);
                    return;
                }

                Graphics graphics;
                Bitmap bmp = Tools.GenerateKeyImage(deviceType, out graphics);
                int height = Tools.GetKeyDefaultHeight(deviceType);
                int width = Tools.GetKeyDefaultWidth(deviceType);

                int fontTitleSize = 8;
                int fontSecondSize = 10;

                if (deviceType == StreamDeckDeviceType.StreamDeckXL)
                {
                    fontTitleSize = 12;
                    fontSecondSize = 14;
                }

                var fontTitle = new Font("Verdana", fontTitleSize, FontStyle.Bold);
                var fontSecond = new Font("Verdana", fontSecondSize, FontStyle.Bold);

                // Background
                var bgBrush = new SolidBrush(ColorTranslator.FromHtml(BACKGROUND_COLOR));
                var fgBrush = Brushes.White;
                graphics.FillRectangle(bgBrush, 0, 0, width, height);

                // Top title
                string title = $"⚫ {streamInfo.Game}";
                graphics.DrawString(title, fontTitle, fgBrush, new PointF(3, 10));

                title = $"⛑ {streamInfo.Viewers}";
                graphics.DrawString(title, fontSecond, fgBrush, new PointF(3, 28));

                var span = DateTime.UtcNow - streamInfo.StreamStart;
                title = span.Hours > 0 ? $"⛣ {span.Hours}:{span.Minutes.ToString("00")}" : $"⛣ {span.Minutes}m";
                graphics.DrawString(title, fontSecond, fgBrush, new PointF(3, 50));
                await Connection.SetImageAsync(bmp);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error drawing currency data {ex}");
            }
        }

        private async void Instance_TokenStatusChanged(object sender, EventArgs e)
        {
            settings.TokenExists = TwitchTokenManager.Instance.TokenExists;
            await SaveSettings();
        }

        private void Chat_PageRaised(object sender, PageRaisedEventArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Received a page! Message: {e.Message?? String.Empty}");
            pageMessage = e.Message;
            isPaging = true;
            fullScreenAlertTriggered = false;
        }

        private void RaiseFullScreenAlert()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Full screen alert: {pageMessage ?? String.Empty}");
            fullScreenAlertTriggered = true;
            AlertManager.Instance.PageMessage = pageMessage;
            AlertManager.Instance.InitFlash();

            if (deviceType == StreamDeckDeviceType.StreamDeckClassic)
            {
                Connection.SwitchProfileAsync("FullScreenAlert");
            }
            else
            {
                Connection.SwitchProfileAsync("FullScreenAlertXL");
            }           
        }

        private void Instance_TwitchStreamInfoChanged(object sender, TwitchStreamInfoEventArgs e)
        {
            streamInfo = e.StreamInfo;
        }

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        // Used to register and revoke token
        private async void StreamDeckConnection_OnSendToPlugin(object sender, streamdeck_client_csharp.StreamDeckEventReceivedEventArgs<streamdeck_client_csharp.Events.SendToPluginEvent> e)
        {
            var payload = e.Event.Payload;
            if (Connection.ContextId != e.Event.Context)
            {
                return;
            }

            if (payload["property_inspector"] != null)
            {
                switch (payload["property_inspector"].ToString().ToLower())
                {
                    case "updateapproval":
                        string approvalCode = (string)payload["approvalCode"];
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"Requesting approval with code: {approvalCode}");
                        TwitchTokenManager.Instance.SetToken(new TwitchToken() { Token = approvalCode, TokenLastRefresh = DateTime.Now });
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"RefreshToken completed. Token Exists: {TwitchTokenManager.Instance.TokenExists}");
                        break;
                    case "resetplugin":
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"ResetPlugin called. Tokens are cleared");
                        TwitchTokenManager.Instance.RevokeToken();
                        await SaveSettings();
                        break;
                }
            }
        }

        private void ResetChat()
        {
            List<string> allowedPagers = null;

            if (!String.IsNullOrWhiteSpace(settings.AllowedPagers))
            {
                allowedPagers = settings.AllowedPagers?.Replace("\r\n", "\n").Split('\n').ToList();
            }
            TwitchChat.Instance.Initalize(settings.PageCooldown, allowedPagers);
        }
        #endregion
    }
}
