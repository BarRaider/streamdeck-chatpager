using BarRaider.SdTools;
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

        #endregion

        public AlertFlasher(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            AlertManager.Instance.FlashStatusChanged += Instance_FlashStatusChanged;
            var deviceInfo = payload.DeviceInfo.Devices.Where(d => d.Id == connection.DeviceId).FirstOrDefault();

            if (deviceInfo != null)
            {
                stringMessageIndex = (deviceInfo.Size.Cols * payload.Coordinates.Row) + payload.Coordinates.Column;
            }

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
            
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            
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


            if (String.IsNullOrEmpty(pageMessage) || stringMessageIndex >= pageMessage?.Length)
            {
                Connection.SetImageAsync(pageArr[index]);
            }
            else
            {
                string letter = pageMessage[stringMessageIndex].ToString();

                Image img = Tools.Base64StringToImage(pageArr[index]);
                Graphics graphics = Graphics.FromImage(img);
                var font = new Font("Verdana", 100, FontStyle.Bold);
                var fgBrush = Brushes.White;

                SizeF stringSize = graphics.MeasureString(letter, font);
                float stringPosX = 0;
                float stringPosY = 0;
                if (stringSize.Width < img.Width)
                {
                    stringPosX = Math.Abs((img.Width - stringSize.Width)) / 2;
                    stringPosY = Math.Abs((img.Height - stringSize.Height)) / 2;
                }
                graphics.DrawString(letter, font, fgBrush, new PointF(stringPosX, stringPosY));
                Connection.SetImageAsync(img);
            }
        }
    }
}
