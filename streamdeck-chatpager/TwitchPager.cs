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
using System.IO;

namespace ChatPager
{
    [PluginActionId("com.barraider.twitchpager")]
    public class TwitchPager : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    TokenExists = false,
                    PageCooldown = 30,
                    AllowedPagers = String.Empty,
                    ChatMessage = String.Empty,
                    DashboardOnClick = true,
                    FullScreenAlert = true,
                    TwoLettersPerKey = false,
                    AlwaysAlert = false,
                    SaveToFile = false,
                    AlertColor = "#FF0000",
                    PageFileName = String.Empty,
                    ClearFileSeconds = DEFAULT_CLEAR_FILE_SECONDS.ToString(),
                    FilePrefix = String.Empty,
                    MultipleChannels = false,
                    MonitoredStreamers = String.Empty
                };
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

            [JsonProperty(PropertyName = "saveToFile")]
            public bool SaveToFile { get; set; }

            [JsonProperty(PropertyName = "pageFileName")]
            public string PageFileName { get; set; }

            [JsonProperty(PropertyName = "filePrefix")]
            public string FilePrefix { get; set; }

            [JsonProperty(PropertyName = "clearFileSeconds")]
            public string ClearFileSeconds { get; set; }

            [JsonProperty(PropertyName = "multipleChannels")]
            public bool MultipleChannels { get; set; }

            [JsonProperty(PropertyName = "monitoredStreamers")]
            public string MonitoredStreamers { get; set; }

            [JsonProperty(PropertyName = "alwaysAlert")]
            public bool AlwaysAlert { get; set; }
        }

        #region Private members

        private const string BACKGROUND_COLOR = "#8560db";
        protected const int DEFAULT_CLEAR_FILE_SECONDS = 5;

        private PluginSettings settings;
        private bool isPaging = false;
        private bool autoClearFile = false;
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
            AlertManager.Instance.TwitchPagerShown += Instance_TwitchPagerShown;
            TwitchChat.Instance.PageRaised += Chat_PageRaised;

            this.settings.ChatMessage = TwitchChat.Instance.ChatMessage;
            settings.TokenExists = TwitchTokenManager.Instance.TokenExists;
            AlertManager.Instance.Initialize(Connection);
            ResetChat();
            deviceType = Connection.DeviceInfo().Type;
            
            tmrPage.Interval = 200;
            tmrPage.Elapsed += TmrPage_Elapsed;
            SaveSettings();
            Connection.GetGlobalSettingsAsync();
        }

        private void Instance_TwitchPagerShown(object sender, EventArgs e)
        {
        }

        #region PluginBase Implementation

        public override void Dispose()
        {
            TwitchStreamInfoManager.Instance.TwitchStreamInfoChanged -= Instance_TwitchStreamInfoChanged;
            TwitchTokenManager.Instance.TokenStatusChanged -= Instance_TokenStatusChanged;
            Connection.StreamDeckConnection.OnSendToPlugin -= StreamDeckConnection_OnSendToPlugin;
            TwitchChat.Instance.PageRaised -= Chat_PageRaised;
            AlertManager.Instance.TwitchPagerShown -= Instance_TwitchPagerShown;
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
                settings.FullScreenAlert = global.FullScreenAlert;
                settings.TwoLettersPerKey = global.TwoLettersPerKey;
                settings.AlwaysAlert = global.AlwaysAlert;
                settings.AlertColor = global.InitialAlertColor;
                settings.SaveToFile = global.SaveToFile;
                settings.PageFileName = global.PageFileName;
                settings.FilePrefix = global.FilePrefix;
                settings.ClearFileSeconds = global.ClearFileSeconds;
                SetClearTimerInterval();
                SaveSettings();
            }
            else // Global settings do not exist
            {
                global = new TwitchGlobalSettings();
                settings.ChatMessage = TwitchChat.Instance.ChatMessage;
                SetGlobalSettings();
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            // Populate new values
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SetClearTimerInterval();
            ResetChat();
            SetGlobalSettings();
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
            global.FullScreenAlert = settings.FullScreenAlert;
            global.TwoLettersPerKey = settings.TwoLettersPerKey;
            global.AlwaysAlert = settings.AlwaysAlert;
            global.InitialAlertColor = settings.AlertColor;
            global.SaveToFile = settings.SaveToFile;
            global.PageFileName = settings.PageFileName;
            global.FilePrefix = settings.FilePrefix;
            global.ClearFileSeconds = settings.ClearFileSeconds;
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
            List<string> monitoredStreamers = null;

            if (!String.IsNullOrWhiteSpace(settings.AllowedPagers))
            {
                allowedPagers = settings.AllowedPagers?.Replace("\r\n", "\n").Split('\n').ToList();
            }

            if (settings.MultipleChannels)
            {
                if (string.IsNullOrWhiteSpace(settings.MonitoredStreamers))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "MultipleChannels is enabled but MonitoredStreamers is empty");
                }
                else
                {
                    monitoredStreamers = settings.MonitoredStreamers?.Replace("\r\n", "\n").Split('\n').ToList();
                }
            }

            TwitchChat.Instance.Initialize(settings.PageCooldown, allowedPagers, monitoredStreamers);
        }

        private void SetClearTimerInterval()
        {
            if (!int.TryParse(settings.ClearFileSeconds, out int value))
            {
                settings.ClearFileSeconds = DEFAULT_CLEAR_FILE_SECONDS.ToString();
                SaveSettings();
            }
        }
        
        #endregion
    }
}
