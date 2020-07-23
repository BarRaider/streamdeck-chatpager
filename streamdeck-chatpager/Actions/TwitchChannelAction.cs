using BarRaider.SdTools;
using ChatPager.Twitch;
using ChatPager.Wrappers;
using NAudio.Wave;
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
                    ShowViewersCount = false
                };
                return instance;
            }

            [JsonProperty(PropertyName = "channelName")]
            public string ChannelName { get; set; }

            [JsonProperty(PropertyName = "hideChannelName")]
            public bool HideChannelName { get; set; }

            [Obsolete("Should no longer be used")]
            [JsonProperty(PropertyName = "hideChannelPreview")]
            public bool HideChannelPreview { get; set; }

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
            if (!String.IsNullOrEmpty(Settings.ChannelName))
            {
                System.Diagnostics.Process.Start(String.Format("https://www.twitch.tv/{0}", Settings.ChannelName));
            }
            else
            {
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

        private Bitmap FetchImage(string imageUrl)
        {
            try
            {
                if (String.IsNullOrEmpty(imageUrl))
                {
                    return null;
                }

                using (WebClient client = new WebClient())
                {
                    using (Stream stream = client.OpenRead(imageUrl))
                    {
                        Bitmap image = new Bitmap(stream);
                        return image;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Failed to fetch image: {imageUrl} {ex}");
            }
            return null;
        }

        private async Task DrawKey()
        {
            if (String.IsNullOrEmpty(Settings.ChannelName) || thumbnailImage == null)
            {
                await Connection.SetImageAsync((String)null);
                return;
            }

            Bitmap bmp = Tools.GenerateGenericKeyImage(out Graphics graphics);
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
                        if (isLive)
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
                    PlaySoundOnLive();
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
                            thumbnailImage = FetchImage(userInfo.ProfileImageUrl.Replace(PREVIEW_IMAGE_WIDTH_TOKEN, PREVIEW_IMAGE_WIDTH_PIXELS.ToString()).Replace(PREVIEW_IMAGE_HEIGHT_TOKEN, PREVIEW_IMAGE_HEIGHT_PIXELS.ToString()));
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
                        thumbnailImage = FetchImage(channelInfo.ThumbnailUrl.Replace(PREVIEW_IMAGE_WIDTH_TOKEN, PREVIEW_IMAGE_WIDTH_PIXELS.ToString()).Replace(PREVIEW_IMAGE_HEIGHT_TOKEN, PREVIEW_IMAGE_HEIGHT_PIXELS.ToString()));
                        lastImageUpdate = DateTime.Now;
                    }
                    else if (channelInfo != null && Settings.LiveGameIcon) // User wants to see the game icon
                    {
                        if (!String.IsNullOrEmpty(channelInfo.GameId))
                        {
                            var gameInfo = await TwitchChannelInfoManager.Instance.GetGameInfo(channelInfo.GameId);
                            if (gameInfo != null)
                            {
                                thumbnailImage = FetchImage(gameInfo.ImageUrl.Replace(PREVIEW_IMAGE_WIDTH_TOKEN, PREVIEW_IMAGE_WIDTH_PIXELS.ToString()).Replace(PREVIEW_IMAGE_HEIGHT_TOKEN, PREVIEW_IMAGE_HEIGHT_PIXELS.ToString()));
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
                            thumbnailImage = FetchImage(userInfo.ProfileImageUrl.Replace(PREVIEW_IMAGE_WIDTH_TOKEN, PREVIEW_IMAGE_WIDTH_PIXELS.ToString()).Replace(PREVIEW_IMAGE_HEIGHT_TOKEN, PREVIEW_IMAGE_HEIGHT_PIXELS.ToString()));
                            lastImageUpdate = DateTime.Now;
                        }
                    }
                }
            }
        }

        private void InitializeSettings()
        {
            SetLiveImageSetting();
            PropagatePlaybackDevices();
        }

        private void PropagatePlaybackDevices()
        {
            Settings.PlaybackDevices = new List<PlaybackDevice>();

            try
            {
                if (Settings.PlaySoundOnLive)
                {
                    for (int idx = -1; idx < WaveOut.DeviceCount; idx++)
                    {
                        var currDevice = WaveOut.GetCapabilities(idx);
                        Settings.PlaybackDevices.Add(new PlaybackDevice() { ProductName = currDevice.ProductName });
                    }

                    Settings.PlaybackDevices = Settings.PlaybackDevices.OrderBy(p => p.ProductName).ToList();
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error propagating playback devices {ex}");
            }
        }

        private void PlaySoundOnLive()
        {
            Task.Run(() =>
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
                var deviceNumber = GetPlaybackDeviceFromDeviceName(Settings.PlaybackDevice);
                using (var audioFile = new AudioFileReader(Settings.PlaySoundOnLiveFile))
                {
                    using (var outputDevice = new WaveOutEvent())
                    {
                        outputDevice.DeviceNumber = deviceNumber;
                        outputDevice.Init(audioFile);
                        outputDevice.Play();
                        while (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                }
            });
        }

        private int GetPlaybackDeviceFromDeviceName(string deviceName)
        {
            for (int idx = -1; idx < WaveOut.DeviceCount; idx++)
            {
                var currDevice = WaveOut.GetCapabilities(idx);
                if (deviceName == currDevice.ProductName)
                {
                    return idx;
                }
            }
            return -1;
        }

        private void SetLiveImageSetting()
        {
            // Check if at least one of these settings are enabled, if it is - no need to do anything
            if (Settings.LiveUserIcon || Settings.LiveStreamPreview || Settings.LiveGameIcon)
            {
                return;
            }

            // Should run once when moving to version Twitch Tools 2.3 (backwards compatibility)
            if (Settings.HideChannelPreview)
            {
                Settings.LiveUserIcon = true;
            }
            else
            {
                Settings.LiveStreamPreview = true;
            }
            SaveSettings();
        }

        #endregion
    }
}
