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
using ChatPager.Backend;

namespace ChatPager.Actions
{

    //---------------------------------------------------
    //          BarRaider's Hall Of Fame
    // Subscriber: CarstenPet
    //---------------------------------------------------
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
                    PageCommand = DEFAULT_PAGE_COMMAND,
                    PubsubNotifications = false,
                    BitsChatMessage = BITS_CHAT_DEFAULT_MESSAGE,
                    BitsFlashColor = BITS_FLASH_DEFAULT_COLOR,
                    BitsFlashMessage = BITS_FLASH_DEFAULT_MESSAGE,
                    FollowChatMessage = FOLLOW_CHAT_DEFAULT_MESSAGE,
                    FollowFlashColor = FOLLOW_FLASH_DEFAULT_COLOR,
                    FollowFlashMessage = FOLLOW_FLASH_DEFAULT_MESSAGE,
                    SubChatMessage = SUB_CHAT_DEFAULT_MESSAGE,
                    SubFlashColor = SUB_FLASH_DEFAULT_COLOR,
                    SubFlashMessage = SUB_FLASH_DEFAULT_MESSAGE,
                    PointsChatMessage = POINTS_CHAT_DEFAULT_MESSAGE,
                    PointsFlashColor = POINTS_FLASH_DEFAULT_COLOR,
                    PointsFlashMessage = POINTS_FLASH_DEFAULT_MESSAGE,
                    RaidChatMessage = RAID_CHAT_DEFAULT_MESSAGE,
                    RaidFlashColor = RAID_FLASH_DEFAULT_COLOR,
                    RaidFlashMessage = RAID_FLASH_DEFAULT_MESSAGE,
                    AutoStopPage = DEFAULT_AUTO_STOP_SECONDS.ToString(),
                    PlaySoundOnChat = false,
                    PlaySoundOnNotification = false,
                    PlaybackDevice = String.Empty,
                    PlaybackDevices = null,
                    PlaySoundFile = String.Empty,
                    SoundCooldown = DEFAULT_SOUND_COOLDOWN_SECONDS.ToString(),
                    MutedUsers = String.Empty
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

            [JsonProperty(PropertyName = "pubsubNotifications")]
            public bool PubsubNotifications { get; set; }

            [JsonProperty(PropertyName = "bitsFlashColor")]
            public string BitsFlashColor { get; set; }

            [JsonProperty(PropertyName = "bitsFlashMessage")]
            public string BitsFlashMessage { get; set; }

            [JsonProperty(PropertyName = "bitsChatMessage")]
            public string BitsChatMessage { get; set; }

            [JsonProperty(PropertyName = "followFlashColor")]
            public string FollowFlashColor { get; set; }

            [JsonProperty(PropertyName = "followFlashMessage")]
            public string FollowFlashMessage { get; set; }

            [JsonProperty(PropertyName = "followChatMessage")]
            public string FollowChatMessage { get; set; }

            [JsonProperty(PropertyName = "subFlashColor")]
            public string SubFlashColor { get; set; }

            [JsonProperty(PropertyName = "subFlashMessage")]
            public string SubFlashMessage { get; set; }

            [JsonProperty(PropertyName = "subChatMessage")]
            public string SubChatMessage { get; set; }

            [JsonProperty(PropertyName = "pointsFlashColor")]
            public string PointsFlashColor { get; set; }

            [JsonProperty(PropertyName = "pointsFlashMessage")]
            public string PointsFlashMessage { get; set; }

            [JsonProperty(PropertyName = "pointsChatMessage")]
            public string PointsChatMessage { get; set; }

            [JsonProperty(PropertyName = "raidFlashColor")]
            public string RaidFlashColor { get; set; }

            [JsonProperty(PropertyName = "raidFlashMessage")]
            public string RaidFlashMessage { get; set; }

            [JsonProperty(PropertyName = "raidChatMessage")]
            public string RaidChatMessage { get; set; }

            [JsonProperty(PropertyName = "autoStopPage")]
            public string AutoStopPage { get; set; }

            [JsonProperty(PropertyName = "playSoundOnChat")]
            public bool PlaySoundOnChat { get; set; }

            [JsonProperty(PropertyName = "playSoundOnNotification")]
            public bool PlaySoundOnNotification { get; set; }

            [JsonProperty(PropertyName = "playbackDevices")]
            public List<PlaybackDevice> PlaybackDevices { get; set; }

            [JsonProperty(PropertyName = "playbackDevice")]
            public string PlaybackDevice { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "playSoundFile")]
            public string PlaySoundFile { get; set; }

            [JsonProperty(PropertyName = "soundCooldown")]
            public string SoundCooldown { get; set; }

            [JsonProperty(PropertyName = "mutedUsers")]
            public string MutedUsers { get; set; }
            
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
        private const string BITS_CHAT_DEFAULT_MESSAGE = "{DISPLAYNAME} cheered {BITS} bits! Overall cheered {TOTALBITS} bits. {MESSAGE}";
        private const string BITS_FLASH_DEFAULT_COLOR = "#FF00FF";
        private const string BITS_FLASH_DEFAULT_MESSAGE = "{DISPLAYNAME} - {BITS} bits";
        private const string FOLLOW_CHAT_DEFAULT_MESSAGE = "Thanks for the follow, @{DISPLAYNAME} !!!";
        private const string FOLLOW_FLASH_DEFAULT_COLOR = "#0000FF";
        private const string FOLLOW_FLASH_DEFAULT_MESSAGE = "Follower: {DISPLAYNAME}";
        private const string SUB_CHAT_DEFAULT_MESSAGE = "New Sub by: @{DISPLAYNAME} for {MONTHS} months!!! {MESSAGE}";
        private const string SUB_FLASH_DEFAULT_COLOR = "#FF0000";
        private const string SUB_FLASH_DEFAULT_MESSAGE = "Sub: {DISPLAYNAME}";
        private const string POINTS_CHAT_DEFAULT_MESSAGE = "{DISPLAYNAME} redeemed {TITLE} for {POINTS} points. {MESSAGE}";
        private const string POINTS_FLASH_DEFAULT_COLOR = "#00FF00";
        private const string POINTS_FLASH_DEFAULT_MESSAGE = "Points: {DISPLAYNAME} - {TITLE}";
        private const string RAID_CHAT_DEFAULT_MESSAGE = "{DISPLAYNAME} just raided with {VIEWERS} viewers!";
        private const string RAID_FLASH_DEFAULT_COLOR = "#FFA500";
        private const string RAID_FLASH_DEFAULT_MESSAGE = "{DISPLAYNAME} raiding with {VIEWERS} viewers";

        protected const int DEFAULT_CLEAR_FILE_SECONDS = 5;
        private const int DEFAULT_AUTO_STOP_SECONDS = 0;
        private const int DEFAULT_SOUND_COOLDOWN_SECONDS = 10;

        private bool isPaging = false;
        private readonly System.Timers.Timer tmrPage = new System.Timers.Timer();
        private int alertStage = 0;
        private TwitchStreamInfo streamInfo;
        private string pageMessage = null;
        private TwitchGlobalSettings global = null;
        private int previousViewersCount = 0;
        private Brush viewersBrush = Brushes.White;
        private bool globalSettingsLoaded = false;
        private int autoStopPage = 0;
        private int soundCooldown = DEFAULT_SOUND_COOLDOWN_SECONDS;
        private DateTime pageStartTime;
        private DateTime lastSoundPlay = DateTime.MinValue;
        private List<String> mutedUsers;

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
            TwitchChat.Instance.OnChatMessageReceived += Instance_OnChatMessageReceived;

            this.Settings.ChatMessage = TwitchChat.Instance.ChatMessage;
            this.Settings.PageCommand = TwitchChat.Instance.PageCommand;
            Settings.TokenExists = TwitchTokenManager.Instance.TokenExists;
            AlertManager.Instance.Initialize(Connection);
            ResetChat();
            InitializeStreamInfo();
           
            tmrPage.Interval = 200;
            tmrPage.Elapsed += TmrPage_Elapsed;
            SaveSettings();
            Connection.GetGlobalSettingsAsync();
        }

        private async void Instance_OnChatMessageReceived(object sender, ChatMessageReceivedEventArgs e)
        {
            if (Settings.PlaySoundOnChat)
            {
                await HandleSoundPlay(e.Author);
            }
        }

        private void Instance_TwitchPagerShown(object sender, EventArgs e)
        {
        }

        #region PluginBase Implementation

        public override void Dispose()
        {
            tmrPage.Stop();
            MyTwitchChannelInfo.Instance.TwitchStreamInfoChanged -= Instance_TwitchStreamInfoChanged;
            TwitchChat.Instance.OnChatMessageReceived -= Instance_OnChatMessageReceived;
            TwitchChat.Instance.PageRaised -= Chat_PageRaised;
            AlertManager.Instance.TwitchPagerShown -= Instance_TwitchPagerShown;
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
                pageStartTime = DateTime.Now;
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
            globalSettingsLoaded = true;
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
                Settings.PubsubNotifications = global.PubsubNotifications;
                Settings.BitsChatMessage = global.BitsChatMessage;
                Settings.BitsFlashColor = global.BitsFlashColor;
                Settings.BitsFlashMessage = global.BitsFlashMessage;
                Settings.FollowChatMessage = global.FollowChatMessage;
                Settings.FollowFlashColor = global.FollowFlashColor;
                Settings.FollowFlashMessage = global.FollowFlashMessage;
                Settings.SubChatMessage = global.SubChatMessage;
                Settings.SubFlashColor = global.SubFlashColor;
                Settings.SubFlashMessage = global.SubFlashMessage;
                Settings.PointsChatMessage = global.PointsChatMessage;
                Settings.PointsFlashColor = global.PointsFlashColor;
                Settings.PointsFlashMessage = global.PointsFlashMessage;
                Settings.RaidChatMessage = global.RaidChatMessage;
                Settings.RaidFlashColor = global.RaidFlashColor;
                Settings.RaidFlashMessage = global.RaidFlashMessage;
                Settings.AutoStopPage = global.AutoStopPage;
                Settings.PlaybackDevice = global.PlaybackDevice;
                Settings.PlaySoundOnChat = global.PlaySoundOnChat;
                Settings.PlaySoundOnNotification = global.PlaySoundOnNotification;
                Settings.PlaySoundFile = global.PlaySoundFile;
                Settings.SoundCooldown = global.SoundCooldown.ToString();
                Settings.MutedUsers = global.MutedUsers;
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
                InitializeSettings();
                SaveSettings();

                if (Settings.PubsubNotifications)
                {
                    TwitchPubSubManager.Instance.Initialize();
                }
                else
                {
                    TwitchPubSubManager.Instance.Disconnect();
                }
            }
            else // Global settings do not exist
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "TwitchPagerAction: Global Settings do not exist!");
                Settings.ChatMessage = TwitchChat.Instance.ChatMessage;
                Settings.PageCommand = TwitchChat.Instance.PageCommand;
                SetGlobalSettings();
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            bool previousPubsubNotifications = Settings.PubsubNotifications;
            // Populate new values
            Tools.AutoPopulateSettings(Settings, payload.Settings);
            InitializeSettings();
            ResetChat();

            if (previousPubsubNotifications != Settings.PubsubNotifications && Settings.PubsubNotifications) // Enabled checkbox
            {
                ResetNotificationMessages();
            }

            Logger.Instance.LogMessage(TracingLevel.INFO, "TwitchPagerAction ReceivedSettings calling SetGlobalSettings");
            SetGlobalSettings();
        }

        #endregion

        #region Private Members

        private bool SetGlobalSettings()
        {
            if (!globalSettingsLoaded)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Ignoring SetGlobalSettings as they were not yet loaded");
                return false;
            }

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
            global.PubsubNotifications = Settings.PubsubNotifications;
            global.BitsChatMessage = Settings.BitsChatMessage;
            global.BitsFlashColor = Settings.BitsFlashColor;
            global.BitsFlashMessage = Settings.BitsFlashMessage;
            global.FollowChatMessage = Settings.FollowChatMessage;
            global.FollowFlashColor = Settings.FollowFlashColor;
            global.FollowFlashMessage = Settings.FollowFlashMessage;
            global.SubChatMessage = Settings.SubChatMessage;
            global.SubFlashColor = Settings.SubFlashColor;
            global.SubFlashMessage = Settings.SubFlashMessage;
            global.PointsChatMessage = Settings.PointsChatMessage;
            global.PointsFlashColor = Settings.PointsFlashColor;
            global.PointsFlashMessage = Settings.PointsFlashMessage;
            global.RaidChatMessage = Settings.RaidChatMessage;
            global.RaidFlashColor = Settings.RaidFlashColor;
            global.RaidFlashMessage = Settings.RaidFlashMessage;
            global.AutoStopPage = Settings.AutoStopPage;
            global.PlaybackDevice = Settings.PlaybackDevice;
            global.PlaySoundOnChat = Settings.PlaySoundOnChat;
            global.PlaySoundOnNotification = Settings.PlaySoundOnNotification;
            global.PlaySoundFile = Settings.PlaySoundFile;
            global.ViewersBrush = viewersBrush.ToHex();
            global.PreviousViewersCount = previousViewersCount;
            global.SoundCooldown = soundCooldown;
            global.MutedUsers = Settings.MutedUsers;
            Connection.SetGlobalSettingsAsync(JObject.FromObject(global));

            return true;
        }

        private void TmrPage_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (autoStopPage > 0 && (DateTime.Now - pageStartTime).TotalSeconds > autoStopPage)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Auto stopping page after {(DateTime.Now - pageStartTime).TotalSeconds} seconds");
                isPaging = false;
                pageMessage = null;
                return;
            }

            using (Bitmap img = Tools.GenerateGenericKeyImage(out Graphics graphics))
            {
                int height = img.Height;
                int width = img.Width;

                // Background

                var bgBrush = new SolidBrush(GraphicsTools.GenerateColorShades(Settings.AlertColor, alertStage, Constants.ALERT_TOTAL_SHADES));
                graphics.FillRectangle(bgBrush, 0, 0, width, height);

                if (String.IsNullOrEmpty(pageMessage))
                {
                    Connection.SetImageAsync(img);
                }
                else
                {
                    using (Font font = new Font("Verdana", 26, FontStyle.Bold, GraphicsUnit.Pixel))
                    {
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
                            pageMessage = Tools.SplitStringToFit(pageMessage, new BarRaider.SdTools.Wrappers.TitleParameters(font.FontFamily, font.Style, font.Size, Color.White, true, BarRaider.SdTools.Wrappers.TitleVerticalAlignment.Top), imageWidthPixels: width);
                        }
                        graphics.DrawString(pageMessage, font, fgBrush, new PointF(stringPos, stringHeight));
                        Connection.SetImageAsync(img);
                    }
                }
                alertStage = (alertStage + 1) % Constants.ALERT_TOTAL_SHADES;
                graphics.Dispose();
            }
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

                using (Bitmap bmp = Tools.GenerateGenericKeyImage(out Graphics graphics))
                {
                    int height = bmp.Height;
                    int width = bmp.Width;

                    int fontTitleSize = 24;
                    int fontSecondSize = 26;
                    int heightPadding = 10;

                    Font fontTitle = new Font("Verdana", fontTitleSize, FontStyle.Bold, GraphicsUnit.Pixel);
                    Font fontSecond = new Font("Verdana", fontSecondSize, FontStyle.Bold, GraphicsUnit.Pixel);

                    // Background
                    var bgBrush = new SolidBrush(ColorTranslator.FromHtml(BACKGROUND_COLOR));
                    var fgBrush = Brushes.White;
                    graphics.FillRectangle(bgBrush, 0, 0, width, height);

                    // Top title
                    string title = $"⚫ {streamInfo.Game}";
                    var nextHeight = graphics.DrawAndMeasureString(title, fontTitle, fgBrush, new PointF(3, 10)) + heightPadding;
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
                        Logger.Instance.LogMessage(TracingLevel.INFO, "TwitchPagerAction DrawStreamData calling SetGlobalSettings");

                        if (!SetGlobalSettings()) // Reset this so it will push the valid value when Global Settings exist
                        {
                            previousViewersCount = 0;
                        }
                    }

                    title = $"⛑ {streamInfo.Viewers}";
                    nextHeight = graphics.DrawAndMeasureString(title, fontSecond, viewersBrush, new PointF(3, nextHeight)) + heightPadding;

                    var span = DateTime.UtcNow - streamInfo.StreamStart;
                    title = span.Hours > 0 ? $"⛣ {span.Hours}:{span.Minutes.ToString("00")}" : $"⛣ {span.Minutes}m";
                    nextHeight = graphics.DrawAndMeasureString(title, fontSecond, fgBrush, new PointF(3, nextHeight));
                    await Connection.SetImageAsync(bmp);
                    fontTitle.Dispose();
                    fontSecond.Dispose();
                    graphics.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error drawing currency data {ex}");
            }
        }

        private async void Chat_PageRaised(object sender, PageRaisedEventArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Received a page! Message: {e.Message?? String.Empty}");
            pageMessage = e.Message;
            isPaging = true;
            if (Settings.PlaySoundOnNotification)
            {
                await HandleSoundPlay(e.Author);
            }
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
                allowedPagers = Settings.AllowedPagers?.Replace("\r\n", "\n").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
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

        private void InitializeSettings()
        {
            if (!Int32.TryParse(Settings.AutoStopPage, out autoStopPage))
            {
                Settings.AutoStopPage = DEFAULT_AUTO_STOP_SECONDS.ToString();
                SetGlobalSettings();
            }

            if (!Int32.TryParse(Settings.SoundCooldown, out soundCooldown))
            {
                Settings.SoundCooldown = DEFAULT_SOUND_COOLDOWN_SECONDS.ToString();
                soundCooldown = DEFAULT_SOUND_COOLDOWN_SECONDS;
                SetGlobalSettings();
            }

            mutedUsers = null;
            if (!String.IsNullOrEmpty(Settings.MutedUsers))
            {
                mutedUsers = Settings.MutedUsers?.Replace("\r\n", "\n").ToLowerInvariant().Split('\n').ToList();
            }

            PropagatePlaybackDevices();
            SetClearTimerInterval();
        }

        private void SetClearTimerInterval()
        {
            if (!int.TryParse(Settings.ClearFileSeconds, out _))
            {
                Settings.ClearFileSeconds = DEFAULT_CLEAR_FILE_SECONDS.ToString();
                SetGlobalSettings();
            }
        }

        private void InitializeStreamInfo()
        {
            streamInfo = MyTwitchChannelInfo.Instance.StreamInfo;
            if (streamInfo == null)
            {
                MyTwitchChannelInfo.Instance.ForceStreamInfoRefresh();
            }
        }

        private void ResetNotificationMessages()
        {
            Settings.BitsChatMessage = BITS_CHAT_DEFAULT_MESSAGE;
            Settings.BitsFlashColor = BITS_FLASH_DEFAULT_COLOR;
            Settings.BitsFlashMessage = BITS_FLASH_DEFAULT_MESSAGE;
            Settings.FollowChatMessage = FOLLOW_CHAT_DEFAULT_MESSAGE;
            Settings.FollowFlashColor = FOLLOW_FLASH_DEFAULT_COLOR;
            Settings.FollowFlashMessage = FOLLOW_FLASH_DEFAULT_MESSAGE;
            Settings.SubChatMessage = SUB_CHAT_DEFAULT_MESSAGE;
            Settings.SubFlashColor = SUB_FLASH_DEFAULT_COLOR;
            Settings.SubFlashMessage = SUB_FLASH_DEFAULT_MESSAGE;
            Settings.PointsChatMessage = POINTS_CHAT_DEFAULT_MESSAGE;
            Settings.PointsFlashColor = POINTS_FLASH_DEFAULT_COLOR;
            Settings.PointsFlashMessage = POINTS_FLASH_DEFAULT_MESSAGE;
            Settings.RaidChatMessage = RAID_CHAT_DEFAULT_MESSAGE;
            Settings.RaidFlashColor = RAID_FLASH_DEFAULT_COLOR;
            Settings.RaidFlashMessage = RAID_FLASH_DEFAULT_MESSAGE;
        }

        private void PropagatePlaybackDevices()
        {
            Settings.PlaybackDevices = new List<PlaybackDevice>();

            try
            {
                if (Settings.PlaySoundOnChat || Settings.PlaySoundOnNotification)
                {
                    Settings.PlaybackDevices = AudioUtils.Common.GetAllPlaybackDevices(true).Select(d => new PlaybackDevice() { ProductName = d }).ToList();
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error propagating playback devices {ex}");
            }
        }

        private async Task HandleSoundPlay(string triggeredUser)
        {
            if (String.IsNullOrEmpty(Settings.PlaySoundFile) || string.IsNullOrEmpty(Settings.PlaybackDevice))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"HandleSoundPlay called but File or Playback device are empty. File: {Settings.PlaySoundFile} Device: {Settings.PlaybackDevice}");
                return;
            }

            if (!File.Exists(Settings.PlaySoundFile))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"HandleSoundPlay called but file does not exist: {Settings.PlaySoundFile}");
                return;
            }

            if (mutedUsers != null && mutedUsers.Contains(triggeredUser.ToLowerInvariant()))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"HandleSoundPlay called but {triggeredUser} is muted");
                return;
            }

            if ((DateTime.Now - lastSoundPlay).TotalSeconds < soundCooldown)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"HandleSoundPlay in cooldown");
                return;
            }

            


            Logger.Instance.LogMessage(TracingLevel.INFO, $"HandleSoundPlay called. Playing {Settings.PlaySoundFile} on device: {Settings.PlaybackDevice}");
            lastSoundPlay = DateTime.Now;
            await AudioUtils.Common.PlaySound(Settings.PlaySoundFile, Settings.PlaybackDevice);
        }

        #endregion
    }
}
