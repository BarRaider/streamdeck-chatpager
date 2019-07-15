using BarRaider.SdTools;
using ChatPager.Twitch;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager
{
    [PluginActionId("com.barraider.alertflasher")]
    class AlertFlasher : PluginBase
    {
        #region Private Members

        private int stringMessageIndex;
        private int deviceColumns = 0;
        private int locationRow = 0;
        private int locationColumn = 0;
        private bool twoLettersPerKey;
        private readonly StreamDeckDeviceType deviceType;


        #endregion

        public AlertFlasher(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            AlertManager.Instance.FlashStatusChanged += Instance_FlashStatusChanged;
            var deviceInfo = payload.DeviceInfo.Devices.Where(d => d.Id == connection.DeviceId).FirstOrDefault();

            stringMessageIndex = -1;
            if (deviceInfo != null && payload?.Coordinates != null)
            {
                deviceColumns = deviceInfo.Size.Cols;
                locationRow = payload.Coordinates.Row;
                locationColumn = payload.Coordinates.Column;
            }
            deviceType = Connection.DeviceInfo().Type;
            Connection.GetGlobalSettingsAsync();
        }

        public override void Dispose()
        {
            AlertManager.Instance.FlashStatusChanged -= Instance_FlashStatusChanged;
        }

        public override void KeyPressed(KeyPayload payload)
        {
            AlertManager.Instance.StopFlash();
            Connection.SwitchProfileAsync(null);
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
            stringMessageIndex = multiplicationFactor * ((deviceColumns * locationRow) + locationColumn);
        }

        private void Instance_FlashStatusChanged(object sender, FlashStatusEventArgs e)
        {
            FlashImage(e.FlashMessage, e.FlashColor);
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
