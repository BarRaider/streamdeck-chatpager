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

namespace ChatPager
{
    [PluginActionId("com.barraider.twitchpager")]
    public class TwitchPager : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.TokenExists = false;
                instance.PageCooldown = 30;
                instance.AllowedPagers = String.Empty;

                return instance;
            }

            [JsonProperty(PropertyName = "tokenExists")]
            public bool TokenExists { get; set; }

            [JsonProperty(PropertyName = "pageCooldown")]
            public int PageCooldown { get; set; }

            [JsonProperty(PropertyName = "allowedPagers")]
            public string AllowedPagers { get; set; }
        }

        #region Private members

        private const string BACKGROUND_COLOR = "#8560db";

        private PluginSettings settings;
        private bool isPaging = false;
        private System.Timers.Timer tmrPage = new System.Timers.Timer();
        private readonly string[] pageArr = { Properties.Settings.Default.TwitchPage1, Properties.Settings.Default.TwitchPage2, Properties.Settings.Default.TwitchPage3, Properties.Settings.Default.TwitchPage2 };
        private int pageIdx = 0;
        private TwitchStreamInfo streamInfo;
        private string pageMessage = null;

        #endregion

        public TwitchPager(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
            TwitchStreamInfoManager.Instance.TwitchStreamInfoChanged += Instance_TwitchStreamInfoChanged;
            TwitchTokenManager.Instance.TokenStatusChanged += Instance_TokenStatusChanged;
            Connection.StreamDeckConnection.OnSendToPlugin += StreamDeckConnection_OnSendToPlugin;
            TwitchChat.Instance.PageRaised += Chat_PageRaised;

            settings.TokenExists = TwitchTokenManager.Instance.TokenExists;
            ResetChat();
            
            tmrPage.Interval = 200;
            tmrPage.Elapsed += TmrPage_Elapsed;
            SaveSettings();
        }

        #region PluginBase Implementation

        public override void Dispose()
        {
            TwitchStreamInfoManager.Instance.TwitchStreamInfoChanged -= Instance_TwitchStreamInfoChanged;
            TwitchTokenManager.Instance.TokenStatusChanged -= Instance_TokenStatusChanged;
            Connection.StreamDeckConnection.OnSendToPlugin -= StreamDeckConnection_OnSendToPlugin;
            TwitchChat.Instance.PageRaised -= Chat_PageRaised;
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor Called");
        }

        public override void KeyPressed(KeyPayload payload)
        {         
            if (isPaging)
            {
                isPaging = false;
                pageMessage = null;
            }
            else
            {
                if (!TwitchStreamInfoManager.Instance.IsLive)
                {
                    TwitchStreamInfoManager.Instance.ForceStreamInfoRefresh();
                }
                else if (TwitchTokenManager.Instance.User != null)
                {
                    System.Diagnostics.Process.Start(String.Format("https://www.twitch.tv/{0}/dashboard/live", TwitchTokenManager.Instance.User.UserName));
                }
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public async override void OnTick()
        {
            if (isPaging && !tmrPage.Enabled)
            {
                pageIdx = 0;
                tmrPage.Start();
            }
            else if (!isPaging)
            {
                tmrPage.Stop();
                DrawStreamData();
            }
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            ResetChat();
        }

        #endregion

        #region Private Members

        private void TmrPage_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (String.IsNullOrEmpty(pageMessage))
            {
                Connection.SetImageAsync(pageArr[pageIdx]);
            }
            else
            {
                Image img = Tools.Base64StringToImage(pageArr[pageIdx]);
                Graphics graphics = Graphics.FromImage(img);
                var font = new Font("Verdana", 12, FontStyle.Bold);
                var fgBrush = Brushes.White;
                SizeF stringSize = graphics.MeasureString(pageMessage, font);
                float stringPos = 0;
                if (stringSize.Width < Tools.KEY_DEFAULT_WIDTH)
                {
                    stringPos = Math.Abs((Tools.KEY_DEFAULT_WIDTH - stringSize.Width)) / 2;
                }
                graphics.DrawString(pageMessage, font, fgBrush, new PointF(stringPos, 54));
                Connection.SetImageAsync(img);
            }
            pageIdx = (pageIdx + 1) % pageArr.Length;
        }

        private async void DrawStreamData()
        {
            try
            {
                if (!TwitchStreamInfoManager.Instance.IsLive || streamInfo == null)
                {
                    await Connection.SetImageAsync(Properties.Settings.Default.TwitchNotLive).ConfigureAwait(false);
                    return;
                }

                Graphics graphics;
                Bitmap bmp = Tools.GenerateKeyImage(out graphics);

                var fontTitle = new Font("Verdana", 8, FontStyle.Bold);
                var fontSecond = new Font("Verdana", 10, FontStyle.Bold);

                // Background
                var bgBrush = new SolidBrush(ColorTranslator.FromHtml(BACKGROUND_COLOR));
                var fgBrush = Brushes.White;
                graphics.FillRectangle(bgBrush, 0, 0, Tools.KEY_DEFAULT_WIDTH, Tools.KEY_DEFAULT_HEIGHT);

                // Top title
                string title = $"⚫ {streamInfo.Game}";
                graphics.DrawString(title, fontTitle, fgBrush, new PointF(3, 10));

                title = $"⛑ {streamInfo.Viewers}";
                graphics.DrawString(title, fontSecond, fgBrush, new PointF(3, 28));

                var span = DateTime.UtcNow - streamInfo.StreamStart;
                title = span.Hours > 0 ? $"⛣ {span.Hours}:{span.Minutes.ToString("00")}" : $"⛣ {span.Minutes}m";
                graphics.DrawString(title, fontSecond, fgBrush, new PointF(3, 50));
                await Connection.SetImageAsync(bmp);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error drawing currency data {ex}");
            }
        }

        private async void Instance_TokenStatusChanged(object sender, EventArgs e)
        {
            settings.TokenExists = TwitchTokenManager.Instance.TokenExists;
            await SaveSettings();
        }

        private void Chat_PageRaised(object sender, PageRaisedEventArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Received a page! Message: {e.Message?? String.Empty}");
            pageMessage = e.Message;
            isPaging = true;
        }

        private void Instance_TwitchStreamInfoChanged(object sender, TwitchStreamInfoEventArgs e)
        {
            streamInfo = e.StreamInfo;
        }

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        // Used to register and revoke token
        private async void StreamDeckConnection_OnSendToPlugin(object sender, streamdeck_client_csharp.StreamDeckEventReceivedEventArgs<streamdeck_client_csharp.Events.SendToPluginEvent> e)
        {
            var payload = e.Event.Payload;
            if (Connection.ContextId != e.Event.Context)
            {
                return;
            }

            if (payload["property_inspector"] != null)
            {
                switch (payload["property_inspector"].ToString().ToLower())
                {
                    case "updateapproval":
                        string approvalCode = (string)payload["approvalCode"];
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"Requesting approval with code: {approvalCode}");
                        TwitchTokenManager.Instance.SetToken(new TwitchToken() { Token = approvalCode, TokenLastRefresh = DateTime.Now });
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"RefreshToken completed. Token Exists: {TwitchTokenManager.Instance.TokenExists}");
                        break;
                    case "resetplugin":
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"ResetPlugin called. Tokens are cleared");
                        TwitchTokenManager.Instance.RevokeToken();
                        await SaveSettings();
                        break;
                }
            }
        }

        private void ResetChat()
        {
            List<string> allowedPagers = null;

            if (!String.IsNullOrWhiteSpace(settings.AllowedPagers))
            {
                allowedPagers = settings.AllowedPagers?.Replace("\r\n", "\n").Split('\n').ToList();
            }
            TwitchChat.Instance.Initalize(settings.PageCooldown, allowedPagers);
        }
        #endregion
    }
}
