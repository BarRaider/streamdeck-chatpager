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
    // Subscriber: nubby_ninja
    // 200 Bits: Nachtmeister666
    // Subscriber: icessassin
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
                    HideChannelName = false
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

        private const int PREVIEW_IMAGE_HEIGHT_PIXELS = 144;
        private const int PREVIEW_IMAGE_WIDTH_PIXELS = 144;
        private const string PREVIEW_IMAGE_WIDTH_TOKEN = "{width}";
        private const string PREVIEW_IMAGE_HEIGHT_TOKEN = "{height}";
        private const string IS_LIVE_TYPE = "live";

        private DateTime lastImageUpdate;
        private Image thumbnailImage;
        private bool isLive = false;

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

            using (Font fontChannel = new Font("Arial", 40, FontStyle.Bold))
            {
                using (Font fontIsStreaming = new Font("Webdings", 18, FontStyle.Regular))
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
                isLive = false;
                var channelInfo = await TwitchChannelInfoManager.Instance.GetChannelInfo(Settings.ChannelName);
                if (channelInfo != null)
                {
                    isLive = channelInfo.Type.ToLowerInvariant() == IS_LIVE_TYPE;
                }

                // Should we refresh the image?
                if ((DateTime.Now - lastImageUpdate).TotalSeconds >= 60)
                {
                    if (channelInfo != null)
                    {
                        thumbnailImage = FetchImage(channelInfo.ThumbnailUrl.Replace(PREVIEW_IMAGE_WIDTH_TOKEN, PREVIEW_IMAGE_WIDTH_PIXELS.ToString()).Replace(PREVIEW_IMAGE_HEIGHT_TOKEN, PREVIEW_IMAGE_HEIGHT_PIXELS.ToString()));
                        lastImageUpdate = DateTime.Now;
                    }
                    else // Could not fetch channelInfo
                    {
                        // Get the user info to fetch the image
                        var userInfo = await TwitchUserInfoManager.Instance.GetUserInfo(Settings.ChannelName);
                        if (userInfo != null)
                        {
                            thumbnailImage = FetchImage(userInfo.ProfileImageUrl.Replace(PREVIEW_IMAGE_WIDTH_TOKEN, PREVIEW_IMAGE_WIDTH_PIXELS.ToString()).Replace(PREVIEW_IMAGE_HEIGHT_TOKEN, PREVIEW_IMAGE_HEIGHT_PIXELS.ToString()));
                            lastImageUpdate = DateTime.Now;
                        }
                        else
                        {
                            thumbnailImage = null;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
