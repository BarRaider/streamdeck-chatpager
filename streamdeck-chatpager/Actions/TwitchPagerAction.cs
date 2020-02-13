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
using ChatPager.Wrappers;

namespace ChatPager.Actions
{
    [PluginActionId("com.barraider.twitchtools.twitchpager")]
    public class TwitchPagerAction : ActionBase
    {
        protected class PluginSettings : PluginSettingsBase
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
                    MonitoredStreamers = String.Empty,
                    PageCommand = DEFAULT_PAGE_COMMAND

                };
                return instance;
            }

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

            [JsonProperty(PropertyName = "pageCommand")]
            public string PageCommand { get; set; }            
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

        #region Private members

        private const string BACKGROUND_COLOR = "#8560db";
        private const string GREEN_COLOR = "#00FF00";
        private const string DEFAULT_PAGE_COMMAND = "!page";
        protected const int DEFAULT_CLEAR_FILE_SECONDS = 5;


        private bool isPaging = false;
        private readonly bool autoClearFile = false;
        private readonly System.Timers.Timer tmrPage = new System.Timers.Timer();
        private int alertStage = 0;
        private TwitchStreamInfo streamInfo;
        private string pageMessage = null;
        private bool fullScreenAlertTriggered = false;
        private readonly StreamDeckDeviceType deviceType;
        private TwitchGlobalSettings global = null;
        private int previousViewersCount = 0;
        private Brush viewersBrush = Brushes.White;

        #endregion     

        public TwitchPagerAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.Settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.Settings = payload.Settings.ToObject<PluginSettings>();
            }
            MyTwitchChannelInfo.Instance.TwitchStreamInfoChanged += Instance_TwitchStreamInfoChanged;
            
            AlertManager.Instance.TwitchPagerShown += Instance_TwitchPagerShown;
            TwitchChat.Instance.PageRaised += Chat_PageRaised;

            this.Settings.ChatMessage = TwitchChat.Instance.ChatMessage;
            this.Settings.PageCommand = TwitchChat.Instance.PageCommand;
            Settings.TokenExists = TwitchTokenManager.Instance.TokenExists;
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
            MyTwitchChannelInfo.Instance.TwitchStreamInfoChanged -= Instance_TwitchStreamInfoChanged;
            TwitchChat.Instance.PageRaised -= Chat_PageRaised;
            AlertManager.Instance.TwitchPagerShown -= Instance_TwitchPagerShown;
            tmrPage.Stop();
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor Called");
        }

        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
            if (isPaging)
            {
                isPaging = false;
                pageMessage = null;
            }
            else
            {
                if (!MyTwitchChannelInfo.Instance.IsLive || streamInfo == null)
                {
                    MyTwitchChannelInfo.Instance.ForceStreamInfoRefresh();
                }
                else if (TwitchTokenManager.Instance.User != null && Settings.DashboardOnClick)
                {
                    System.Diagnostics.Process.Start(String.Format("https://www.twitch.tv/{0}/dashboard/live", TwitchTokenManager.Instance.User.UserName));
                }
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

            if (isPaging && !tmrPage.Enabled && !Settings.FullScreenAlert)
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
                TwitchChat.Instance.SetPageCommand(global.PageCommand);
                Settings.ChatMessage = TwitchChat.Instance.ChatMessage;
                Settings.PageCommand = TwitchChat.Instance.PageCommand;
                Settings.FullScreenAlert = global.FullScreenAlert;
                Settings.TwoLettersPerKey = global.TwoLettersPerKey;
                Settings.AlwaysAlert = global.AlwaysAlert;
                Settings.AlertColor = global.InitialAlertColor;
                Settings.SaveToFile = global.SaveToFile;
                Settings.PageFileName = global.PageFileName;
                Settings.FilePrefix = global.FilePrefix;
                Settings.ClearFileSeconds = global.ClearFileSeconds;
                previousViewersCount = global.PreviousViewersCount;
                if (!String.IsNullOrEmpty(global.ViewersBrush))
                {
                    try
                    {
                        viewersBrush = new SolidBrush(ColorTranslator.FromHtml(global.ViewersBrush));
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"Invalid global ViewersBrush {global.ViewersBrush} - {ex}");
                    }
                }
                SetClearTimerInterval();
                SaveSettings();
            }
            else // Global settings do not exist
            {
                global = new TwitchGlobalSettings();
                Settings.ChatMessage = TwitchChat.Instance.ChatMessage;
                Settings.PageCommand = TwitchChat.Instance.PageCommand;
                SetGlobalSettings();
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            // Populate new values
            Tools.AutoPopulateSettings(Settings, payload.Settings);
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

            global.ChatMessage = Settings.ChatMessage;
            global.PageCommand = Settings.PageCommand;
            global.FullScreenAlert = Settings.FullScreenAlert;
            global.TwoLettersPerKey = Settings.TwoLettersPerKey;
            global.AlwaysAlert = Settings.AlwaysAlert;
            global.InitialAlertColor = Settings.AlertColor;
            global.SaveToFile = Settings.SaveToFile;
            global.PageFileName = Settings.PageFileName;
            global.FilePrefix = Settings.FilePrefix;
            global.ClearFileSeconds = Settings.ClearFileSeconds;
            global.ViewersBrush = viewersBrush.ToHex();
            global.PreviousViewersCount = previousViewersCount;
            Connection.SetGlobalSettingsAsync(JObject.FromObject(global));
        }

        private void TmrPage_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Bitmap img = Tools.GenerateKeyImage(deviceType, out Graphics graphics);
            int height = Tools.GetKeyDefaultHeight(deviceType);
            int width = Tools.GetKeyDefaultWidth(deviceType);

            // Background
            
            var bgBrush = new SolidBrush(Helpers.GenerateStageColor(Settings.AlertColor, alertStage, Helpers.TOTAL_ALERT_STAGES));
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
            graphics.Dispose();
        }

        private async void DrawStreamData()
        {
            try
            {
                if (!MyTwitchChannelInfo.Instance.IsLive || streamInfo == null)
                {
                    await Connection.SetImageAsync(Properties.Settings.Default.TwitchNotLive).ConfigureAwait(false);
                    return;
                }

                Bitmap bmp = Tools.GenerateKeyImage(deviceType, out Graphics graphics);
                int height = Tools.GetKeyDefaultHeight(deviceType);
                int width = Tools.GetKeyDefaultWidth(deviceType);

                int fontTitleSize = 8;
                int fontSecondSize = 10;

                if (deviceType == StreamDeckDeviceType.StreamDeckXL)
                {
                    fontTitleSize = 12;
                    fontSecondSize = 14;
                }

                Font fontTitle = new Font("Verdana", fontTitleSize, FontStyle.Bold);
                var fontSecond = new Font("Verdana", fontSecondSize, FontStyle.Bold);

                // Background
                var bgBrush = new SolidBrush(ColorTranslator.FromHtml(BACKGROUND_COLOR));
                var fgBrush = Brushes.White;
                graphics.FillRectangle(bgBrush, 0, 0, width, height);

                // Top title
                string title = $"⚫ {streamInfo.Game}";
                graphics.DrawString(title, fontTitle, fgBrush, new PointF(3, 10));

                // Figure out which color to use for the viewers
                if (streamInfo.Viewers != previousViewersCount)
                {
                    if (streamInfo.Viewers < previousViewersCount)
                    {
                        viewersBrush = Brushes.Red;
                    }
                    else
                    {
                        viewersBrush = new SolidBrush(ColorTranslator.FromHtml(GREEN_COLOR));
                    }
                    previousViewersCount = streamInfo.Viewers;
                    SetGlobalSettings();
                }

                title = $"⛑ {streamInfo.Viewers}";
                graphics.DrawString(title, fontSecond, viewersBrush, new PointF(3, 28));

                var span = DateTime.UtcNow - streamInfo.StreamStart;
                title = span.Hours > 0 ? $"⛣ {span.Hours}:{span.Minutes.ToString("00")}" : $"⛣ {span.Minutes}m";
                graphics.DrawString(title, fontSecond, fgBrush, new PointF(3, 50));
                await Connection.SetImageAsync(bmp);
                graphics.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error drawing currency data {ex}");
            }
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

        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        private void ResetChat()
        {
            List<string> allowedPagers = null;
            List<string> monitoredStreamers = null;

            if (!String.IsNullOrWhiteSpace(Settings.AllowedPagers))
            {
                allowedPagers = Settings.AllowedPagers?.Replace("\r\n", "\n").Split('\n').ToList();
            }

            if (Settings.MultipleChannels)
            {
                if (string.IsNullOrWhiteSpace(Settings.MonitoredStreamers))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "MultipleChannels is enabled but MonitoredStreamers is empty");
                }
                else
                {
                    monitoredStreamers = Settings.MonitoredStreamers?.Replace("\r\n", "\n").Split('\n').ToList();
                }
            }

            TwitchChat.Instance.InitializePager(Settings.PageCooldown, allowedPagers, monitoredStreamers);
        }

        private void SetClearTimerInterval()
        {
            if (!int.TryParse(Settings.ClearFileSeconds, out _))
            {
                Settings.ClearFileSeconds = DEFAULT_CLEAR_FILE_SECONDS.ToString();
                SaveSettings();
            }
        }
        
        #endregion
    }
}
