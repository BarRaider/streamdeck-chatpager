using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatPager.Twitch
{
    public class MyTwitchChannelInfo
    {
        #region Private Members
        private static MyTwitchChannelInfo instance = null;
        private static readonly object objLock = new object();

        private const int MINIMAL_FORCE_REFRESH_MS = 5000; // 5 seconds
        private const int DEFAULT_REFRESH_MS = 60000; // 60 seconds
        private const int MAX_REFRESH_MS     = 300000; // 300 seconds = 5 min

        private TwitchStreamInfo lastStreamInfo;
        private DateTime lastStreamInfoRefresh;
        private readonly TwitchComm comm;
        private readonly System.Timers.Timer tmrFetchStreamInfo;
        private readonly SemaphoreSlim refreshLock = new SemaphoreSlim(1, 1);

        #endregion

        #region Public Events

        public event EventHandler<TwitchStreamInfoEventArgs> TwitchStreamInfoChanged;
        public bool IsLive {
            get
            {
                return lastStreamInfo != null && lastStreamInfo.StreamType == "live";
            }
        }

        #endregion


        #region Constructors

        public static MyTwitchChannelInfo Instance
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
                        instance = new MyTwitchChannelInfo();
                    }
                    return instance;
                }
            }
        }

        private MyTwitchChannelInfo()
        {
            comm = new TwitchComm();
            TwitchTokenManager.Instance.TokensChanged += Instance_TokensChanged;
            tmrFetchStreamInfo = new System.Timers.Timer();
            ResetTimerInterval();
            tmrFetchStreamInfo.Elapsed += TmrFetchStreamInfo_Elapsed;
            tmrFetchStreamInfo.Start();
            GetStreamInfo();
        }

        #endregion

        #region Public Methods

        public TwitchStreamInfo StreamInfo
        {
            get
            {
                return lastStreamInfo;
            }
        }

        public DateTime LastStreamInfoRefresh
        {
            get
            {
                return lastStreamInfoRefresh;
            }
        }

        public async void ForceStreamInfoRefresh()
        {
            await refreshLock.WaitAsync();
            try
            {
                if ((DateTime.Now - lastStreamInfoRefresh).TotalMilliseconds > MINIMAL_FORCE_REFRESH_MS)
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, "Force Stream Info");
                    GetStreamInfo();
                }
            }
            finally
            {
                refreshLock.Release();
            }
        }

        #endregion

        #region Private Methods

        private void TmrFetchStreamInfo_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            GetStreamInfo();
        }

        private void Instance_TokensChanged(object sender, TwitchTokenEventArgs e)
        {
            if (e.Token != null && !String.IsNullOrEmpty(e.Token.Token))
            {
                GetStreamInfo();
            }
        }

        private void ResetTimerInterval()
        {
            tmrFetchStreamInfo.Interval = DEFAULT_REFRESH_MS;
        }

        private void IncreaseTimerInterval()
        {
            double interval = tmrFetchStreamInfo.Interval;

            if (interval < DEFAULT_REFRESH_MS)
            {
                interval = DEFAULT_REFRESH_MS;
            }

            interval += DEFAULT_REFRESH_MS;
            if (interval > MAX_REFRESH_MS)
            {
                interval = MAX_REFRESH_MS;
            }
            tmrFetchStreamInfo.Interval = interval;
        }

        private async void GetStreamInfo()
        {
            if (TwitchStreamInfoChanged != null && TwitchTokenManager.Instance.User != null && !string.IsNullOrEmpty(TwitchTokenManager.Instance.User.UserId))
            {
                lastStreamInfo = await comm.GetMyStreamInfo();
                if (lastStreamInfo != null)
                {

                    if (tmrFetchStreamInfo.Interval != DEFAULT_REFRESH_MS)
                    {
                        ResetTimerInterval();
                    }
                    lastStreamInfoRefresh = DateTime.Now;
                    TwitchStreamInfoChanged?.Invoke(this, new TwitchStreamInfoEventArgs(lastStreamInfo));
                    return;
                }
                    
                IncreaseTimerInterval();
                TwitchStreamInfoChanged?.Invoke(this, new TwitchStreamInfoEventArgs(null));
            }
        }
        #endregion
    }
}
