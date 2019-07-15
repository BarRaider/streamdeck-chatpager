using BarRaider.SdTools;
using ChatPager.Twitch;
using System;
using System.Drawing;

namespace ChatPager
{
    public class AlertManager
    {

        #region Private Members
        private const string DEFAULT_ALERT_COLOR = "#FF0000";

        private static AlertManager instance = null;
        private static readonly object objLock = new object();

        private System.Timers.Timer tmrPage = new System.Timers.Timer();
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
            GlobalSettingsManager.Instance.OnReceivedGlobalSettings += Instance_OnReceivedGlobalSettings;
            //tmrPage.Start();
        }

        private void Instance_OnReceivedGlobalSettings(object sender, ReceivedGlobalSettingsPayload payload)
        {
            if (payload?.Settings != null)
            {
                TwitchGlobalSettings global = payload.Settings.ToObject<TwitchGlobalSettings>();
                initialAlertColor = global.InitialAlertColor;
            }
        }

        private void TmrPage_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (String.IsNullOrEmpty(initialAlertColor))
            {
                initialAlertColor = DEFAULT_ALERT_COLOR;
            }

            FlashStatusChanged?.Invoke(this, new FlashStatusEventArgs(Helpers.GenerateStageColor(initialAlertColor, alertStage, Helpers.TOTAL_ALERT_STAGES), PageMessage));
            alertStage = (alertStage + 1) % Helpers.TOTAL_ALERT_STAGES;
        }

        #endregion

        #region Public Methods

        public event EventHandler<FlashStatusEventArgs> FlashStatusChanged;

        public string PageMessage { get; set; }

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

        #endregion
    }
}
