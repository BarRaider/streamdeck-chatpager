using BarRaider.SdTools;
using System;

namespace ChatPager
{
    public class AlertManager
    {

        #region Private Members

        private static AlertManager instance = null;
        private static readonly object objLock = new object();

        private System.Timers.Timer tmrPage = new System.Timers.Timer();
        private int pageIdx = 0;

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
            //tmrPage.Start();
        }

        private void TmrPage_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            FlashStatusChanged?.Invoke(this, new FlashStatusEventArgs(pageIdx, PageMessage));

            pageIdx = (pageIdx + 1) % 4;
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
            FlashStatusChanged?.Invoke(this, new FlashStatusEventArgs(-1, null));
        }

        #endregion
    }
}
