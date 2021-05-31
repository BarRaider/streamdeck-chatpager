using BarRaider.SdTools;
using ChatPager.Backend;
using ChatPager.Twitch;
using ChatPager.Wrappers;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ChatPager
{
    public class AlertManager
    {

        #region Private Members
        private const string DEFAULT_ALERT_COLOR = "#FF0000";

        private static AlertManager instance = null;
        private static readonly object objLock = new object();

        private readonly System.Timers.Timer tmrPage = new System.Timers.Timer();
        private readonly System.Timers.Timer tmrClearFile = new System.Timers.Timer();
        private int alertStage = 0;
        private DateTime pageStartTime;
        private string currentPageInitialColor = DEFAULT_ALERT_COLOR;

        private string pageMessage;
        private ISDConnection connection;
        private TwitchGlobalSettings global;
        private bool autoClearFile = false;
        private int numberOfKeys;
        private ActiveStreamersEventArgs streamersEventArgs = null;
        private ChatMessageListEventArgs chatMessageEventArgs = null;
        private int autoStopSeconds = 0;

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
            tmrPage.Interval = 330;
            tmrPage.Elapsed += TmrPage_Elapsed;
            tmrClearFile.Elapsed += TmrClearFile_Elapsed;
            GlobalSettingsManager.Instance.OnReceivedGlobalSettings += Instance_OnReceivedGlobalSettings;
            GlobalSettingsManager.Instance.RequestGlobalSettings();
            TwitchChat.Instance.PageRaised += Chat_PageRaised;
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

        #region Public Methods
        public void Initialize(ISDConnection connection)
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
            pageStartTime = DateTime.Now;
            tmrPage.Start();
        }

        public void StopFlashAndReset()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"StopFlash called");
            tmrPage.Stop();

            if (FlashStatusChanged == null)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "StopFlash called but FlashStatusChanged is null");
            }
            FlashStatusChanged?.Invoke(this, new FlashStatusEventArgs(Color.Empty, null));
            Thread.Sleep(100);
        }

        public async void ShowActiveStreamers(TwitchActiveStreamer[] streamers, TwitchLiveStreamersLongPressAction longPressAction)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"ShowActiveStreamers called");
            StopFlashAndReset();

            await SwitchToFullScreen();

            // Wait until the UI Action keys have subscribed to get events
            int retries = 0;
            while (!IsReady && retries < 60)
            {
                Thread.Sleep(100);
                retries++;
            }
            if (!IsReady)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Could not get full screen ready!");
                await connection.SwitchProfileAsync(null);
                return;
            }
            StopFlashAndReset();
            streamersEventArgs = new ActiveStreamersEventArgs(streamers, longPressAction, numberOfKeys, 0);

            ActiveStreamersChanged?.Invoke(this, streamersEventArgs);
        }

        public void MoveToNextChatPage()
        {
            if (chatMessageEventArgs == null)
            {
                return;
            }
            StopFlashAndReset();
            chatMessageEventArgs.CurrentPage++;
            ChatMessageListChanged?.Invoke(this, chatMessageEventArgs);
        }

        public void MoveToPrevChatPage()
        {
            if (chatMessageEventArgs == null)
            {
                return;
            }

            if (chatMessageEventArgs.CurrentPage == 0) // Already on first page
            {
                return;
            }


            StopFlashAndReset();
            chatMessageEventArgs.CurrentPage--;
            ChatMessageListChanged?.Invoke(this, chatMessageEventArgs);
        }

        public void MoveToNextStreamersPage()
        {
            if (streamersEventArgs == null)
            {
                return;
            }
            StopFlashAndReset();
            streamersEventArgs.CurrentPage++;
            ActiveStreamersChanged?.Invoke(this, streamersEventArgs);
        }

        public void MoveToPrevStreamersPage()
        {
            if (streamersEventArgs == null)
            {
                return;
            }

            if (streamersEventArgs.CurrentPage == 0) // Already on first page
            {
                return;
            }

            StopFlashAndReset();
            streamersEventArgs.CurrentPage--;
            ActiveStreamersChanged?.Invoke(this, streamersEventArgs);
        }

        public async void ShowChatMessages(ChatMessageKey[] chatMessageKeys, string channel)
        {
            StopFlashAndReset();
            Logger.Instance.LogMessage(TracingLevel.INFO, $"ShowChatMessages called");

            await SwitchToFullScreen();

            // Wait until the UI Action keys have subscribed to get events
            int retries = 0;
            while (!IsReady && retries < 60)
            {
                Thread.Sleep(100);
                retries++;
            }
            if (!IsReady)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Could not get full screen ready!");
                await connection.SwitchProfileAsync(null);
                return;
            }
            StopFlashAndReset();

            chatMessageEventArgs = new ChatMessageListEventArgs(chatMessageKeys, channel, numberOfKeys, 0);
            ChatMessageListChanged?.Invoke(this, chatMessageEventArgs);
        }


        #endregion

        #region Private Methods

        private async void Chat_PageRaised(object sender, PageRaisedEventArgs e)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "AlertManager: Page Raised!");
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

            await SwitchToFullScreen();
        }

        private void Instance_OnReceivedGlobalSettings(object sender, ReceivedGlobalSettingsPayload payload)
        {
            if (payload?.Settings != null)
            {
                global = payload.Settings.ToObject<TwitchGlobalSettings>();
                InitializeSettings();
            }
        }

        private void TmrPage_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (autoStopSeconds > 0 && (DateTime.Now - pageStartTime).TotalSeconds > autoStopSeconds)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Auto stopping page after {(DateTime.Now - pageStartTime).TotalSeconds} seconds");
                StopFlashAndReset();
                connection.SwitchProfileAsync(null);
                return;
            }

            if (String.IsNullOrEmpty(currentPageInitialColor))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "TmrPage: No alert color, reverting to default");
                currentPageInitialColor = DEFAULT_ALERT_COLOR;
            }

            Color shadeColor = GraphicsTools.GenerateColorShades(currentPageInitialColor, alertStage, Constants.ALERT_TOTAL_SHADES);
            FlashStatusChanged?.Invoke(this, new FlashStatusEventArgs(shadeColor, pageMessage));
            alertStage = (alertStage + 1) % Constants.ALERT_TOTAL_SHADES;
        }

        private void SavePageToFile(string pageMessage)
        {
            if (global.SaveToFile)
            {
                if (string.IsNullOrEmpty(global.PageFileName))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "AlertManager: SavePageToFile called but PageFileName is empty");
                    return;
                }

                Logger.Instance.LogMessage(TracingLevel.INFO, $"AlertManager: Saving page message {pageMessage} to file {global.PageFileName}. AutoClearFile is {autoClearFile}");
                File.WriteAllText(global.PageFileName, $"{pageMessage}");

                if (autoClearFile && !String.IsNullOrEmpty(pageMessage))
                {
                    tmrClearFile.Start();
                }
            }
        }
        private void InitializeSettings()
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

            if (!Int32.TryParse(global.AutoStopPage, out autoStopSeconds))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Invalid AutoStopPage value: {global.AutoStopPage}");
                autoStopSeconds = 0;
            }
        }

        private void TmrClearFile_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            tmrClearFile.Stop();
            SavePageToFile(String.Empty);
        }

        private async Task SwitchToFullScreen()
        {
            string profileName = String.Empty;
            switch (connection.DeviceInfo().Type)
            {
                case StreamDeckDeviceType.StreamDeckClassic:
                    profileName = "FullScreenAlert";
                    break;
                case StreamDeckDeviceType.StreamDeckMini:
                    profileName = "FullScreenAlertMini";
                    break;
                case StreamDeckDeviceType.StreamDeckXL:
                    profileName = "FullScreenAlertXL";
                    break;
                case StreamDeckDeviceType.StreamDeckMobile:
                    profileName = "FullScreenAlertMobile";
                    break;
                default:
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"SwitchToFullScreen: Unsupported device type: {connection.DeviceInfo().Type}");
                    break;
            }

            if (!String.IsNullOrEmpty(profileName))
            {
                await connection.SwitchProfileAsync(profileName);
            }
        }

        #endregion
    }
}
