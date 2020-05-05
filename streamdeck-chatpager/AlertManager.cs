using BarRaider.SdTools;
using ChatPager.Backend;
using ChatPager.Twitch;
using ChatPager.Wrappers;
using System;
using System.Drawing;
using System.IO;
using System.Threading;

namespace ChatPager
{
    public class AlertManager
    {

        #region Private Members
        private const string DEFAULT_ALERT_COLOR = "#FF0000";

        private static AlertManager instance = null;
        private static readonly object objLock = new object();

        private System.Timers.Timer tmrPage = new System.Timers.Timer();
        private System.Timers.Timer tmrClearFile = new System.Timers.Timer();
        private int alertStage = 0;
        private string currentPageInitialColor = DEFAULT_ALERT_COLOR;

        #endregion


        #region Constructors

        public static AlertManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                lock (objLock)
                {
                    if (instance == null)
                    {
                        instance = new AlertManager();
                    }
                    return instance;
                }
            }
        }

        private AlertManager()
        {
            tmrPage.Interval = 200;
            tmrPage.Elapsed += TmrPage_Elapsed;
            tmrClearFile.Elapsed += TmrClearFile_Elapsed;
            GlobalSettingsManager.Instance.OnReceivedGlobalSettings += Instance_OnReceivedGlobalSettings;
            GlobalSettingsManager.Instance.RequestGlobalSettings();
            TwitchChat.Instance.PageRaised += Chat_PageRaised;
            //tmrPage.Start();
        }

        #endregion

        #region Public Members

        public event EventHandler<FlashStatusEventArgs> FlashStatusChanged;
        public event EventHandler<ActiveStreamersEventArgs> ActiveStreamersChanged;
        public event EventHandler<ChatMessageListEventArgs> ChatMessageListChanged;
        public event EventHandler TwitchPagerShown;

        public bool IsReady
        {
            get
            {
                bool isReady = ActiveStreamersChanged != null && ActiveStreamersChanged.GetInvocationList().Length >= numberOfKeys;
                if (!isReady)
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"IsReady was called but Subscribers: {ActiveStreamersChanged?.GetInvocationList()?.Length}/{numberOfKeys}");
                }
                return isReady;
            }
        }

        #endregion

        #region Private Members

        private string pageMessage;
        private SDConnection connection;
        private TwitchGlobalSettings global;
        private bool autoClearFile = false;
        private int numberOfKeys;
        private ActiveStreamersEventArgs streamersEventArgs = null;
        private ChatMessageListEventArgs chatMessageEventArgs = null;


        #endregion

        #region Public Methods
        public void Initialize(SDConnection connection)
        {
            this.connection = connection;
            if (connection != null)
            {
                var deviceInfo = connection.DeviceInfo();
                numberOfKeys = deviceInfo.Size.Cols * deviceInfo.Size.Rows;
            }
        }

        public void InitFlash()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"InitFlash called");
            tmrPage.Start();
        }

        public void StopFlash()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"StopFlash called");
            tmrPage.Stop();
            FlashStatusChanged?.Invoke(this, new FlashStatusEventArgs(Color.Empty, null));
        }

        public async void ShowActiveStreamers(TwitchActiveStreamer[] streamers, TwitchLiveStreamersLongPressAction longPressAction)
        {
            StopFlash();
            Logger.Instance.LogMessage(TracingLevel.INFO, $"ShowActiveStreamers called");

            switch (connection.DeviceInfo().Type)
            {
                case StreamDeckDeviceType.StreamDeckClassic:
                    await connection.SwitchProfileAsync("FullScreenAlert");
                    break;
                case StreamDeckDeviceType.StreamDeckMini:
                    await connection.SwitchProfileAsync("FullScreenAlertMini");
                    break;
                case StreamDeckDeviceType.StreamDeckXL:
                    await connection.SwitchProfileAsync("FullScreenAlertXL");
                    break;
                default:
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"SwitchProfileAsync: Unsupported device type: {connection.DeviceInfo().Type}");
                    break;
            }

            // Wait until the GameUI Action keys have subscribed to get events
            int retries = 0;
            while (!IsReady && retries < 100)
            {
                Thread.Sleep(100);
                retries++;
            }
            streamersEventArgs = new ActiveStreamersEventArgs(streamers, longPressAction, numberOfKeys, 0);

            ActiveStreamersChanged?.Invoke(this, streamersEventArgs);
        }

        public void MoveToNextChatPage()
        {
            if (chatMessageEventArgs == null)
            {
                return;
            }
            StopFlash();
            chatMessageEventArgs.CurrentPage++;
            ChatMessageListChanged?.Invoke(this, chatMessageEventArgs);
        }

        public void MoveToNextStreamersPage()
        {
            if (streamersEventArgs == null)
            {
                return;
            }
            StopFlash();
            streamersEventArgs.CurrentPage++;
            ActiveStreamersChanged?.Invoke(this, streamersEventArgs);
        }

        public async void ShowChatMessages(ChatMessageKey[] chatMessageKeys, string channel)
        {
            StopFlash();
            Logger.Instance.LogMessage(TracingLevel.INFO, $"ShowChatMessages called");

            switch (connection.DeviceInfo().Type)
            {
                case StreamDeckDeviceType.StreamDeckClassic:
                    await connection.SwitchProfileAsync("FullScreenAlert");
                    break;
                case StreamDeckDeviceType.StreamDeckMini:
                    await connection.SwitchProfileAsync("FullScreenAlertMini");
                    break;
                case StreamDeckDeviceType.StreamDeckXL:
                    await connection.SwitchProfileAsync("FullScreenAlertXL");
                    break;
                default:
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"SwitchProfileAsync: Unsupported device type: {connection.DeviceInfo().Type}");
                    break;
            }

            // Wait until the GameUI Action keys have subscribed to get events
            int retries = 0;
            while (!IsReady && retries < 100)
            {
                Thread.Sleep(100);
                retries++;
            }

            chatMessageEventArgs = new ChatMessageListEventArgs(chatMessageKeys, channel, numberOfKeys, 0);
            ChatMessageListChanged?.Invoke(this, chatMessageEventArgs);
        }


        #endregion

        #region Private Methods

        private async void Chat_PageRaised(object sender, PageRaisedEventArgs e)
        {
            SavePageToFile($"{global.FilePrefix}{e.Message}");
            if (!global.FullScreenAlert)
            {
                return;
            }

            if (TwitchPagerShown == null && !global.AlwaysAlert)
            {
                return;
            }

            // Set color if defined in page
            if (String.IsNullOrEmpty(e.Color))
            {
                currentPageInitialColor = global?.InitialAlertColor ?? DEFAULT_ALERT_COLOR;
            }
            else
            {
                currentPageInitialColor = e.Color;
            }
            pageMessage = e.Message;

            Logger.Instance.LogMessage(TracingLevel.INFO, $"Full screen alert: {pageMessage ?? String.Empty} Color: {currentPageInitialColor}");
            InitFlash();

            switch (connection.DeviceInfo().Type)
            {
                case StreamDeckDeviceType.StreamDeckClassic:
                    await connection.SwitchProfileAsync("FullScreenAlert");
                    break;
                case StreamDeckDeviceType.StreamDeckMini:
                    await connection.SwitchProfileAsync("FullScreenAlertMini");
                    break;
                case StreamDeckDeviceType.StreamDeckXL:
                    await connection.SwitchProfileAsync("FullScreenAlertXL");
                    break;
                default:
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"SwitchProfileAsync: Unsupported device type: {connection.DeviceInfo().Type}");
                    break;
            }
        }

        private void Instance_OnReceivedGlobalSettings(object sender, ReceivedGlobalSettingsPayload payload)
        {
            if (payload?.Settings != null)
            {
                global = payload.Settings.ToObject<TwitchGlobalSettings>();
                SetClearTimerInterval();
            }
        }

        private void TmrPage_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (String.IsNullOrEmpty(currentPageInitialColor))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "TmrPage: No alert color, reverting to default");
                currentPageInitialColor = DEFAULT_ALERT_COLOR;
            }

            Color shadeColor = GraphicsTools.GenerateColorShades(currentPageInitialColor, alertStage, Constants.ALERT_TOTAL_SHADES);
            FlashStatusChanged?.Invoke(this, new FlashStatusEventArgs(shadeColor, pageMessage));
            alertStage = (alertStage + 1) % Constants.ALERT_TOTAL_SHADES;
        }

        private void SavePageToFile(string pageMessage, bool autoClear = true)
        {
            if (global.SaveToFile)
            {
                if (string.IsNullOrEmpty(global.PageFileName))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "SavePageToFile called but PageFileName is empty");
                    return;
                }

                Logger.Instance.LogMessage(TracingLevel.INFO, $"Saving message {pageMessage} to file {global.PageFileName}");
                File.WriteAllText(global.PageFileName, $"{pageMessage}");

                if (autoClearFile)
                {
                    tmrClearFile.Start();
                }
            }
        }
        private void SetClearTimerInterval()
        {
            autoClearFile = false;
            if (int.TryParse(global.ClearFileSeconds, out int value))
            {
                if (value > 0)
                {
                    autoClearFile = true;
                    tmrClearFile.Interval = value * 1000;
                }
            }
        }

        private void TmrClearFile_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            tmrClearFile.Stop();
            SavePageToFile(string.Empty, false);
        }

        #endregion
    }
}
