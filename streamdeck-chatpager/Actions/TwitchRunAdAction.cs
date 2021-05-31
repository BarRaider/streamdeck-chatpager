using BarRaider.SdTools;
using ChatPager.Twitch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Actions
{
    [PluginActionId("com.barraider.twitchtools.runad")]
    public class TwitchRunAdAction : ActionBase
    {
        protected class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    TokenExists = false,
                    AdLength = DEFAULT_AD_LENGTH.ToString()
                };
                return instance;
            }

            [JsonProperty(PropertyName = "adLength")]
            public string AdLength { get; set; }
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
        private const int DEFAULT_AD_LENGTH = 30;

        private readonly string[] DEFAULT_IMAGES = new string[]
        {
            @"images\adCooldown.png",
            @"images\adAction@2x.png"
        };
        private Image cooldownImage = null;
        private Image adImage = null;
        private int adLength = DEFAULT_AD_LENGTH;

        #endregion

        #region Public Methods

        public TwitchRunAdAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            PrefetchImages();
            InitializeSettings();
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

            if (DateTime.Now < TwitchChannelInfoManager.Instance.NextAdTime)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} called but still in cooldown");
                await Connection.ShowAlert();
                return;
            }

            if (await TwitchChannelInfoManager.Instance.RunAd(adLength))
            {
                await Connection.ShowOk();
                OnTick();
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

            if (!baseHandledOnTick)
            {
                HandleKeyImage();
            }
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(Settings, payload.Settings);
            InitializeSettings();
            SaveSettings();
        }

        #endregion

        #region Private Methods

        private void PrefetchImages()
        {
            if (cooldownImage != null)
            {
                cooldownImage.Dispose();
                cooldownImage = null;
            }

            if (adImage != null)
            {
                adImage.Dispose();
                adImage = null;
            }

            cooldownImage = Image.FromFile(DEFAULT_IMAGES[0]);
            adImage = Image.FromFile(DEFAULT_IMAGES[1]);
        }

        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        private void InitializeSettings()
        {
            if (!Int32.TryParse(Settings.AdLength, out adLength))
            {
                Settings.AdLength = DEFAULT_AD_LENGTH.ToString();
                adLength = DEFAULT_AD_LENGTH;
                SaveSettings();
            }
        }

        private async void HandleKeyImage()
        {
            ImageData imageData;
            if (DateTime.Now < TwitchChannelInfoManager.Instance.AdEndTime)
            {
                // Ad is currently running
                int secondsRemaining = (int)Math.Ceiling((TwitchChannelInfoManager.Instance.AdEndTime - DateTime.Now).TotalSeconds);
                imageData = new ImageData()
                {
                    Background = adImage,
                    Text = secondsRemaining.ToString(),
                    TextColor = Color.Orange,
                    TextSize = 80,
                    TextY = 40
                };
                if (imageData.Text.Length >= 3)
                {
                    imageData.TextX = 27;
                }
                else if (imageData.Text.Length == 2)
                {
                    imageData.TextX = 38;
                }
                else
                {
                    imageData.TextX = 50;
                }

            }
            else if (DateTime.Now < TwitchChannelInfoManager.Instance.NextAdTime)
            {
                // Currently in cooldown mode
                imageData = new ImageData()
                {
                    Background = cooldownImage,
                    Text = $"{(int)(TwitchChannelInfoManager.Instance.NextAdTime - DateTime.Now).TotalMinutes + 1}m",
                    TextColor = Color.Red,
                    TextSize = 60,
                    TextX = 40,
                    TextY = 58
                };
            }
            else
            {
                // Ready to play ad
                imageData = new ImageData()
                {
                    Background = adImage,
                    Text = "ad",
                    TextColor = Color.White,
                    TextSize = 80,
                    TextX = 38,
                    TextY = 40
                };
            }
            await DrawAdKey(imageData);
        }

        private class ImageData
        {
            public Image Background { get; set; }
            public string Text { get; set; }
            public int TextSize { get; set; }
            public Color TextColor { get; set; }
            public int TextX { get; set; }
            public int TextY { get; set; }
        }

        private async Task DrawAdKey(ImageData imageData)
        {
            using (Bitmap bmp = Tools.GenerateGenericKeyImage(out Graphics graphics))
            {
                int height = bmp.Height;
                int width = bmp.Width;

                if (imageData.Background != null)
                {
                    using (Image img = (Image)imageData.Background.Clone())
                    {
                        graphics.DrawImage(img, 0, 0, width, height);
                    }
                }

                using (Font font = new Font("Verdana", imageData.TextSize, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    using (GraphicsPath gpath = new GraphicsPath())
                    {
                        PointF textStart;
                        if (imageData.TextX < 0)
                        {
                            textStart = new PointF(graphics.GetTextCenter(imageData.Text, width, font), imageData.TextY);
                        }
                        else
                        {
                            textStart = new Point(imageData.TextX, imageData.TextY);
                        }
                        gpath.AddString(imageData.Text,
                                            font.FontFamily,
                                            (int)FontStyle.Bold,
                                            graphics.DpiY * font.SizeInPoints / width,
                                            textStart,
                                            new StringFormat());
                        graphics.DrawPath(Pens.Black, gpath);
                        graphics.FillPath(new SolidBrush(imageData.TextColor), gpath);
                    }
                }

                await Connection.SetImageAsync(bmp);
                graphics.Dispose();
            }
        }

        #endregion
    }
}