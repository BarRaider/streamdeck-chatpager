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

        private static readonly string[] pageArr = { Properties.Settings.Default.AlertPage1, Properties.Settings.Default.AlertPage2, Properties.Settings.Default.AlertPage3, Properties.Settings.Default.AlertPage2 };
        private int stringMessageIndex;
        private int deviceColumns = 0;
        private int locationRow = 0;
        private int locationColumn = 0;
        private bool twoLettersPerKey;


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
            FlashImage(e.FlashMessage, e.FlashIndex);
        }

        private void FlashImage(string pageMessage, int index)
        {
            if (index == -1)
            {
                Connection.SetImageAsync((string)null);
                Connection.SetTitleAsync(null);
                return;
            }


            if (String.IsNullOrEmpty(pageMessage) || stringMessageIndex < 0 || stringMessageIndex >= pageMessage?.Length)
            {
                Connection.SetImageAsync(pageArr[index]);
            }
            else
            {
                Image img = Tools.Base64StringToImage(pageArr[index]);
                Graphics graphics = Graphics.FromImage(img);
                var fgBrush = Brushes.White;
                string letter = pageMessage[stringMessageIndex].ToString();

                if (twoLettersPerKey) // 2 Letters per key
                {
                    var font = new Font("Arial", 70, FontStyle.Bold);
                    if (pageMessage.Length > stringMessageIndex + 1)
                    {
                        letter = pageMessage.Substring(stringMessageIndex, 2);
                    }
                    
                    // Draw first letter
                    graphics.DrawString(letter[0].ToString(), font, fgBrush, new PointF(1, 32));

                    if (letter.Length > 1)
                    {
                        graphics.DrawString(letter[1].ToString(), font, fgBrush, new PointF(65, 32));
                    }
                }
                else // 1 Letter per key
                {
                    var font = new Font("Verdana", 100, FontStyle.Bold);
                    SizeF stringSize = graphics.MeasureString(letter, font);
                    float stringPosX = 0;
                    float stringPosY = 0;
                    if (stringSize.Width < img.Width)
                    {
                        stringPosX = Math.Abs((img.Width - stringSize.Width)) / 2;
                        stringPosY = Math.Abs((img.Height - stringSize.Height)) / 2;
                    }
                    graphics.DrawString(letter, font, fgBrush, new PointF(stringPosX, stringPosY));
                }
                Connection.SetImageAsync(img);
            }
        }
    }
}
