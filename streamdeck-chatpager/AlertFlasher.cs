using BarRaider.SdTools;
using ChatPager.Twitch;
using ChatPager.Wrappers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager
{
    [PluginActionId("com.barraider.alertflasher")]
    class AlertFlasher : PluginBase
    {
        #region Private Members

        private const int PREVIEW_IMAGE_HEIGHT_PIXELS = 144;
        private const int PREVIEW_IMAGE_WIDTH_PIXELS = 144;
        private const string PREVIEW_IMAGE_WIDTH_TOKEN = "{width}";
        private const string PREVIEW_IMAGE_HEIGHT_TOKEN = "{height}";

        private int stringMessageIndex;
        private int deviceColumns = 0;
        private int locationRow = 0;
        private int locationColumn = 0;
        private int sequentialKey;
        private bool twoLettersPerKey;
        private bool flashMode = false;
        private string channelName;
        private readonly StreamDeckDeviceType deviceType;


        #endregion

        public AlertFlasher(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            AlertManager.Instance.FlashStatusChanged += Instance_FlashStatusChanged;
            AlertManager.Instance.ActiveStreamersChanged += Instance_ActiveStreamersChanged;
            var deviceInfo = payload.DeviceInfo.Devices.Where(d => d.Id == connection.DeviceId).FirstOrDefault();

            stringMessageIndex = -1;
            sequentialKey = 0;
            if (deviceInfo != null && payload?.Coordinates != null)
            {
                deviceColumns = deviceInfo.Size.Cols;
                locationRow = payload.Coordinates.Row;
                locationColumn = payload.Coordinates.Column;
                sequentialKey = (deviceColumns * locationRow) + locationColumn;
            }
            deviceType = Connection.DeviceInfo().Type;
            Connection.GetGlobalSettingsAsync();
        }

        public override void Dispose()
        {
            AlertManager.Instance.FlashStatusChanged -= Instance_FlashStatusChanged;
            AlertManager.Instance.ActiveStreamersChanged -= Instance_ActiveStreamersChanged;
        }

        public override void KeyPressed(KeyPayload payload)
        {
            if (flashMode || sequentialKey == 0)
            {
                AlertManager.Instance.StopFlash();
                Connection.SwitchProfileAsync(null);
            }
            else if (!String.IsNullOrEmpty(channelName))
            {
                System.Diagnostics.Process.Start(String.Format("https://www.twitch.tv/{0}", channelName));
            }
               
        }

        public override void KeyReleased(KeyPayload payload)
        {
            
        }

        public override void OnTick()
        {
            
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            if (payload?.Settings != null)
            {
                TwitchGlobalSettings global = payload.Settings.ToObject<TwitchGlobalSettings>();
                twoLettersPerKey = global.TwoLettersPerKey;
                CalculateStringIndex();
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            
        }

        private void CalculateStringIndex()
        {
            int multiplicationFactor = twoLettersPerKey ? 2 : 1;
            stringMessageIndex = multiplicationFactor * sequentialKey;
        }

        private void Instance_FlashStatusChanged(object sender, FlashStatusEventArgs e)
        {
            flashMode = true;
            FlashImage(e.FlashMessage, e.FlashColor);
        }

        private async void Instance_ActiveStreamersChanged(object sender, ActiveStreamersEventArgs e)
        {
            flashMode = false;
            if (sequentialKey == 0)
            {
                await Connection.SetTitleAsync("Exit");
            }
            else if (e.ActiveStreamers != null && e.ActiveStreamers.Length + 1 > sequentialKey) 
                // +1 because starting on second key
            {
                var streamerInfo = e.ActiveStreamers[sequentialKey - 1];
                using (Image image = FetchImage(streamerInfo.PreviewImages.Template.Replace(PREVIEW_IMAGE_WIDTH_TOKEN, PREVIEW_IMAGE_WIDTH_PIXELS.ToString()).Replace(PREVIEW_IMAGE_HEIGHT_TOKEN, PREVIEW_IMAGE_HEIGHT_PIXELS.ToString())))
                {
                    await DrawStreamerImage(streamerInfo, image);
                }
                channelName = streamerInfo?.Channel?.Name;
            }
            
        }

        private Image FetchImage(string imageUrl)
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

        private async Task DrawStreamerImage(TwitchActiveStreamer streamerInfo, Image background)
        {
            Bitmap bmp = Tools.GenerateGenericKeyImage(out Graphics graphics);
            int height = bmp.Height;
            int width = bmp.Width;

            var fontChannel = new Font("Arial", 40, FontStyle.Bold);
            var fontViewers = new Font("Verdana", 22, FontStyle.Bold);
            var fontIsStreaming = new Font("Webdings", 18, FontStyle.Regular);
            var fontViewerCount = new Font("Webdings", 24, FontStyle.Regular);
            GraphicsPath gpath = new GraphicsPath();
            int startWidth = 0;

            if (background != null)
            {
                // Draw background
                graphics.DrawImage(background, 0, 0, width, height);
            }

            // Draw Viewer Count
            graphics.DrawString("N", fontViewerCount, Brushes.White, new PointF(3, 8));
            string viewers = $"{streamerInfo.Viewers}";
            //graphics.DrawString(viewers, fontViewers, Brushes.White, new PointF(35, 3));
            gpath.AddString(viewers,
                                fontViewers.FontFamily,
                                (int)FontStyle.Bold,
                                graphics.DpiY * fontChannel.SizeInPoints / width,
                                new Point(35, 3),
                                new StringFormat());

            // Draw Red Circle
            graphics.DrawString("n", fontIsStreaming, Brushes.Red, new Point(3, 110));
            startWidth = 30;

            // Set Streamer Name
            gpath.AddString(streamerInfo.Channel.DisplayName,
                                fontChannel.FontFamily,
                                (int)FontStyle.Bold,
                                graphics.DpiY * fontChannel.SizeInPoints / width,
                                new Point(startWidth, 108),
                                new StringFormat());
            graphics.DrawPath(Pens.Black, gpath);
            graphics.FillPath(Brushes.White, gpath);



            await Connection.SetImageAsync(bmp);
        }

        private void FlashImage(string pageMessage, Color flashColor)
        {
            if (flashColor == Color.Empty)
            {
                Connection.SetImageAsync((string)null);
                Connection.SetTitleAsync(null);
                return;
            }

            Bitmap img = Tools.GenerateKeyImage(deviceType, out Graphics graphics);
            int height = Tools.GetKeyDefaultHeight(deviceType);
            int width = Tools.GetKeyDefaultWidth(deviceType);

            // Magic numbers after a bunch of trial and error :-/

            // For SD Classic
            int twoLetterFontSize = 32;
            int oneLetterFontSize = 50;
            int twoLetterTop = 5;
            int oneLetterTop = 0;
            int twoLetterBuffer = 32;
            if (deviceType == StreamDeckDeviceType.StreamDeckXL)
            {
                twoLetterFontSize = 40;
                twoLetterTop = 15;
                oneLetterTop = 15;
                twoLetterBuffer = 45;
            }

            // Background
            var bgBrush = new SolidBrush(flashColor);
            graphics.FillRectangle(bgBrush, 0, 0, width, height);

            if (String.IsNullOrEmpty(pageMessage) || stringMessageIndex < 0 || stringMessageIndex >= pageMessage?.Length)
            {
                Connection.SetImageAsync(img);
            }
            else
            {
                var fgBrush = Brushes.White;
                string letter = pageMessage[stringMessageIndex].ToString();

                if (twoLettersPerKey) // 2 Letters per key
                {
                    var font = new Font("Arial", twoLetterFontSize, FontStyle.Bold);
                    if (pageMessage.Length > stringMessageIndex + 1)
                    {
                        letter = pageMessage.Substring(stringMessageIndex, 2);
                    }
                    
                    // Draw first letter
                    graphics.DrawString(letter[0].ToString(), font, fgBrush, new PointF(1, twoLetterTop));

                    if (letter.Length > 1)
                    {
                        graphics.DrawString(letter[1].ToString(), font, fgBrush, new PointF(twoLetterBuffer, twoLetterTop));
                    }
                }
                else // 1 Letter per key
                {
                    var font = new Font("Verdana", oneLetterFontSize, FontStyle.Bold);
                    SizeF stringSize = graphics.MeasureString(letter, font);
                    float stringPosX = 0;
                    float stringPosY = oneLetterTop;
                    if (stringSize.Width < img.Width)
                    {
                        stringPosX = Math.Abs((img.Width - stringSize.Width)) / 2;
                    }
                    graphics.DrawString(letter, font, fgBrush, new PointF(stringPosX, stringPosY));
                }
                Connection.SetImageAsync(img);
            }
        }
    }
}
