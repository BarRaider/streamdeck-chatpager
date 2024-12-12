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

    [PluginActionId("com.barraider.twitchtools.channel")]
    public class TwitchChannelAction : ActionBase
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
                    PlaySoundOnLive = false,
                    PlaybackDevices = null,
                    PlaybackDevice = String.Empty,
                    PlaySoundOnLiveFile = string.Empty,
                    GrayscaleImageWhenNotLive = false,
                    LiveStreamPreview = true,
                    LiveGameIcon = false,
                    LiveUserIcon = false,
                    ShowViewersCount = false,
                    HideRecordIcon = false,
                    KeypressDisabled = false,
                    KeypressCreator = false,
                    KeypressMod = false,
                    KeypressStream = false,
                    CustomBrowser = false,
                    KeypressNewWindow = false,
                    KeypressAppMode = false,
                    BrowserExecutableFile = String.Empty
                };
                return instance;
            }

            [JsonProperty(PropertyName = "channelName")]
            public string ChannelName { get; set; }

            [JsonProperty(PropertyName = "hideChannelName")]
            public bool HideChannelName { get; set; }

            [JsonProperty(PropertyName = "playSoundOnLive")]
            public bool PlaySoundOnLive { get; set; }

            [JsonProperty(PropertyName = "playbackDevices")]
            public List<PlaybackDevice> PlaybackDevices { get; set; }

            [JsonProperty(PropertyName = "playbackDevice")]
            public string PlaybackDevice { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "playSoundOnLiveFile")]
            public string PlaySoundOnLiveFile { get; set; }

            [JsonProperty(PropertyName = "grayscaleImageWhenNotLive")]
            public bool GrayscaleImageWhenNotLive { get; set; }

            [JsonProperty(PropertyName = "liveStreamPreview")]
            public bool LiveStreamPreview { get; set; }

            [JsonProperty(PropertyName = "liveGameIcon")]
            public bool LiveGameIcon { get; set; }

            [JsonProperty(PropertyName = "liveUserIcon")]
            public bool LiveUserIcon { get; set; }

            [JsonProperty(PropertyName = "showViewersCount")]
            public bool ShowViewersCount { get; set; }

            [JsonProperty(PropertyName = "hideRecordIcon")]
            public bool HideRecordIcon { get; set; }

            [JsonProperty(PropertyName = "keypressDisabled")]
            public bool KeypressDisabled { get; set; }

            [JsonProperty(PropertyName = "keypressStream")]
            public bool KeypressStream { get; set; }

            [JsonProperty(PropertyName = "keypressMod")]
            public bool KeypressMod { get; set; }

            [JsonProperty(PropertyName = "keypressCreator")]
            public bool KeypressCreator { get; set; }

            [JsonProperty(PropertyName = "customBrowser")]
            public bool CustomBrowser { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "browserExecutableFile")]
            public string BrowserExecutableFile { get; set; }

            [JsonProperty(PropertyName = "keypressNewWindow")]
            public bool KeypressNewWindow { get; set; }

            [JsonProperty(PropertyName = "keypressAppMode")]
            public bool KeypressAppMode { get; set; }

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

        private const string URL_STREAM = "https://www.twitch.tv/{0}";
        private const string URL_MOD = "https://www.twitch.tv/moderator/{0}";
        private const string URL_CREATOR = "https://dashboard.twitch.tv/u/{0}/stream-manager";

        private const int PREVIEW_IMAGE_HEIGHT_PIXELS = 144;
        private const int PREVIEW_IMAGE_WIDTH_PIXELS = 144;
        private const string PREVIEW_IMAGE_WIDTH_TOKEN = "{width}";
        private const string PREVIEW_IMAGE_HEIGHT_TOKEN = "{height}";

        private DateTime lastImageUpdate;
        private Image thumbnailImage;
        private bool isLive = false;
        private bool isFirstTimeLoading = true;
        private bool previouslyWasLive = false;
        private int viewersCount = 0;


        #endregion

        #region Public Methods

        public TwitchChannelAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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

            if (String.IsNullOrEmpty(Settings.ChannelName) || Settings.KeypressDisabled)
            {
                await Connection.ShowAlert();
                return;
            }

            string url = String.Empty;

            if (Settings.KeypressStream)
            {
                url = URL_STREAM;
            }
            else if (Settings.KeypressMod)
            {
                url = URL_MOD;
            }
            else if (Settings.KeypressCreator)
            {
                url = URL_CREATOR;
            }

            if (String.IsNullOrEmpty(url))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"Channel: Invalid Keypress setting, url is null");
                return;
            }

            string startString = String.Format(url, Settings.ChannelName.ToLowerInvariant());
            string argsString = "";

            if (Settings.CustomBrowser && !String.IsNullOrEmpty(Settings.BrowserExecutableFile))
            {
                List<string> args = new List<string>();
                if (Settings.KeypressNewWindow)
                {
                    args.Add("--new-window");
                }

                if (Settings.KeypressAppMode)
                {
                    args.Add(String.Format("--app={0}", startString));
                }
                else
                {
                    args.Add(startString);
                }


                startString = Settings.BrowserExecutableFile;
                argsString = String.Join(" ", args);
            }

            System.Diagnostics.Process.Start(startString, argsString);

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

            if (!String.IsNullOrEmpty(Settings.ChannelName) && TwitchTokenManager.Instance.TokenExists)
            {
                await RefreshChannelData();
                await DrawKey();
            }
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            string channelName = Settings.ChannelName;
            Tools.AutoPopulateSettings(Settings, payload.Settings);

            if (channelName != Settings.ChannelName)
            {
                lastImageUpdate = DateTime.MinValue;
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

        private async Task DrawKey()
        {
            if (String.IsNullOrEmpty(Settings.ChannelName) || thumbnailImage == null)
            {
                await Connection.SetImageAsync((String)null);
                return;
            }

            using (Bitmap bmp = Tools.GenerateGenericKeyImage(out Graphics graphics))
            {
                int height = bmp.Height;
                int width = bmp.Width;

                using (Font fontChannel = new Font("Verdana", 44, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    using (Font fontIsStreaming = new Font("Webdings", 22, FontStyle.Regular, GraphicsUnit.Pixel))
                    {
                        using (GraphicsPath gpath = new GraphicsPath())
                        {
                            int startWidth = 0;
                            if (thumbnailImage != null)
                            {
                                using (Image img = (Image)thumbnailImage.Clone())
                                {
                                    // Draw background
                                    graphics.DrawImage(img, 0, 0, width, height);
                                }
                            }

                            // Draw Red Circle
                            if (isLive && !Settings.HideRecordIcon)
                            {
                                graphics.DrawString("n", fontIsStreaming, Brushes.Red, new Point(3, 110));
                                startWidth = 30;
                            }

                            // Draw Channel name
                            if (!Settings.HideChannelName)
                            {

                                gpath.AddString(Settings.ChannelName,
                                                    fontChannel.FontFamily,
                                                    (int)FontStyle.Bold,
                                                    graphics.DpiY * fontChannel.SizeInPoints / width,
                                                    new Point(startWidth, 108),
                                                    new StringFormat());
                                graphics.DrawPath(Pens.Black, gpath);
                                graphics.FillPath(Brushes.White, gpath);
                            }

                            if (isLive && Settings.ShowViewersCount)
                            {
                                using (Font fontViewersCount = new Font("Webdings", 25, FontStyle.Regular, GraphicsUnit.Pixel))
                                {
                                    // Draw Viewer Count
                                    graphics.DrawString("N", fontViewersCount, Brushes.White, new PointF(3, 8));
                                }
                                string viewers = $"{viewersCount}";
                                gpath.AddString(viewers,
                                                    fontChannel.FontFamily,
                                                    (int)FontStyle.Bold,
                                                    graphics.DpiY * fontChannel.SizeInPoints / width,
                                                    new Point(35, 7),
                                                    new StringFormat());
                                graphics.DrawPath(Pens.Black, gpath);
                                graphics.FillPath(Brushes.White, gpath);
                            }
                        }
                    }
                }

                await Connection.SetImageAsync(bmp);
                graphics.Dispose();
            }
        }

        private async Task RefreshChannelData()
        {
            if (!String.IsNullOrEmpty(Settings.ChannelName))
            {
                previouslyWasLive = isLive;
                isLive = false;
                var channelInfo = await TwitchChannelInfoManager.Instance.GetChannelInfo(Settings.ChannelName);
                if (channelInfo != null)
                {
                    isLive = channelInfo.IsLive;
                    viewersCount = channelInfo.Viewers;
                }

                // Channel just turned live.  Don't make a sound if we just loaded the plugin for the first time
                if (isLive && isLive != previouslyWasLive && !isFirstTimeLoading)
                {
                    await PlaySoundOnLive();
                }
                isFirstTimeLoading = false;

                // Should we refresh the image?
                if ((DateTime.Now - lastImageUpdate).TotalSeconds >= 60)
                {
                    thumbnailImage = null;

                    if (!isLive) // Handle non live streamer
                    {
                        var userInfo = await TwitchUserInfoManager.Instance.GetUserInfo(Settings.ChannelName);
                        if (userInfo != null)
                        {
                            thumbnailImage = await HelperFunctions.FetchImage(HelperFunctions.GenerateUrlFromGenericImageUrl(userInfo.ProfileImageUrl));
                            lastImageUpdate = DateTime.Now;

                            // Make the image grayscale
                            if (Settings.GrayscaleImageWhenNotLive)
                            {
                                thumbnailImage = System.Windows.Forms.ToolStripRenderer.CreateDisabledImage(thumbnailImage);
                            }
                        }
                        return;
                    }

                    // Streamer is live!
                    // Only switch to stream preview if channelInfo is not null AND user wants the stream preview
                    if (channelInfo != null && Settings.LiveStreamPreview)
                    {
                        thumbnailImage = await HelperFunctions.FetchImage(HelperFunctions.GenerateUrlFromGenericImageUrl(channelInfo.ThumbnailURL));
                        lastImageUpdate = DateTime.Now;
                    }
                    else if (channelInfo != null && Settings.LiveGameIcon) // User wants to see the game icon
                    {
                        if (channelInfo.GameId > 0)
                        {
                            var gameInfo = await TwitchChannelInfoManager.Instance.GetGameInfo(channelInfo.GameId);
                            if (gameInfo != null)
                            {
                                thumbnailImage = gameInfo.GameImage;
                                lastImageUpdate = DateTime.Now;
                            }
                        }
                    }
                    else if (Settings.LiveUserIcon) // User wants the user's icon
                    {
                        // Get the user info to fetch the image
                        var userInfo = await TwitchUserInfoManager.Instance.GetUserInfo(Settings.ChannelName);
                        if (userInfo != null)
                        {
                            thumbnailImage = await HelperFunctions.FetchImage(HelperFunctions.GenerateUrlFromGenericImageUrl(userInfo.ProfileImageUrl));
                            lastImageUpdate = DateTime.Now;
                        }
                    }
                }
            }
        }

        private void InitializeSettings()
        {
            PropagatePlaybackDevices();

            // Backwards compatibility
            if (!Settings.KeypressStream && !Settings.KeypressDisabled && !Settings.KeypressCreator && !Settings.KeypressMod)
            {
                Settings.KeypressStream = true;
            }
        }

        private void PropagatePlaybackDevices()
        {
            Settings.PlaybackDevices = new List<PlaybackDevice>();

            try
            {
                if (Settings.PlaySoundOnLive)
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

        private async Task PlaySoundOnLive()
        {
            if (!Settings.PlaySoundOnLive)
            {
                return;
            }

            if (String.IsNullOrEmpty(Settings.PlaySoundOnLiveFile) || string.IsNullOrEmpty(Settings.PlaybackDevice))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"PlaySoundOnLive called but File or Playback device are empty. File: {Settings.PlaySoundOnLiveFile} Device: {Settings.PlaybackDevice}");
                return;
            }

            if (!File.Exists(Settings.PlaySoundOnLiveFile))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"PlaySoundOnLive called but file does not exist: {Settings.PlaySoundOnLiveFile}");
                return;
            }

            Logger.Instance.LogMessage(TracingLevel.INFO, $"PlaySoundOnLive called. Playing {Settings.PlaySoundOnLiveFile} on device: {Settings.PlaybackDevice}");
            await AudioUtils.Common.PlaySound(Settings.PlaySoundOnLiveFile, Settings.PlaybackDevice);
        }

        #endregion
    }
}
