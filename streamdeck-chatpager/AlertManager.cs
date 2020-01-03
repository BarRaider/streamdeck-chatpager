﻿using BarRaider.SdTools;
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

        private string initialAlertColor = null;

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

        public async void ShowActiveStreamers(TwitchActiveStreamer[] streamers)
        {
            StopFlash();
            Logger.Instance.LogMessage(TracingLevel.INFO, $"ShowActiveStreamers called");

            if (connection.DeviceInfo().Type == StreamDeckDeviceType.StreamDeckClassic)
            {
                await connection.SwitchProfileAsync("FullScreenAlert");
            }
            else
            {
                await connection.SwitchProfileAsync("FullScreenAlertXL");
            }

            // Wait until the GameUI Action keys have subscribed to get events
            int retries = 0;
            while (!IsReady && retries < 100)
            {
                Thread.Sleep(100);
                retries++;
            }
            ActiveStreamersChanged?.Invoke(this, new ActiveStreamersEventArgs(streamers));
        }

        #endregion

        #region Private Methods

        private void Chat_PageRaised(object sender, PageRaisedEventArgs e)
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

            pageMessage = e.Message;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Full screen alert: {pageMessage ?? String.Empty}");
            InitFlash();

            if (connection.DeviceInfo().Type == StreamDeckDeviceType.StreamDeckClassic)
            {
                connection.SwitchProfileAsync("FullScreenAlert");
            }
            else
            {
                connection.SwitchProfileAsync("FullScreenAlertXL");
            }
        }

        private void Instance_OnReceivedGlobalSettings(object sender, ReceivedGlobalSettingsPayload payload)
        {
            if (payload?.Settings != null)
            {
                global = payload.Settings.ToObject<TwitchGlobalSettings>();
                initialAlertColor = global.InitialAlertColor;
                SetClearTimerInterval();
            }
        }

        private void TmrPage_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (String.IsNullOrEmpty(initialAlertColor))
            {
                initialAlertColor = DEFAULT_ALERT_COLOR;
            }

            FlashStatusChanged?.Invoke(this, new FlashStatusEventArgs(Helpers.GenerateStageColor(initialAlertColor, alertStage, Helpers.TOTAL_ALERT_STAGES), pageMessage));
            alertStage = (alertStage + 1) % Helpers.TOTAL_ALERT_STAGES;
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