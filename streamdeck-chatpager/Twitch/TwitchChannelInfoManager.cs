using BarRaider.SdTools;
using ChatPager.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatPager.Twitch
{

    //---------------------------------------------------
    //          BarRaider's Hall Of Fame
    // Subscriber: Nachtmeister666
    // Subscriber: JustWeb
    //---------------------------------------------------

    public class TwitchChannelInfoManager
    {
        #region Private Members
        private const int CHANNEL_REFRESH_TIME_SEC = 60;
        private const int ACTIVE_STREAMERS_REFRESH_TIME_SEC = 60;

        private static TwitchChannelInfoManager instance = null;
        private static readonly object objLock = new object();
        private readonly TwitchComm comm;
        private readonly Dictionary<string, TwitchChannelUpdateInfo> dicChannelInfo = new Dictionary<string, TwitchChannelUpdateInfo>();
        private readonly SemaphoreSlim channelInfoLock = new SemaphoreSlim(1, 1);
        private DateTime lastActiveStreamers;
        private TwitchActiveStreamer[] activeStreamers;

        #endregion

        #region Constructors

        public static TwitchChannelInfoManager Instance
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
                        instance = new TwitchChannelInfoManager();
                    }
                    return instance;
                }
            }
        }

        private TwitchChannelInfoManager()
        {
            comm = new TwitchComm();
        }

        public async Task<TwitchChannelInfo> GetChannelInfo(string channelName)
        {
            if (String.IsNullOrEmpty(channelName))
            {
                return null;
            }

            await channelInfoLock.WaitAsync();
            try
            {

                channelName = channelName.ToLowerInvariant();
                // Check if the information we have about a channel is current
                if (dicChannelInfo.ContainsKey(channelName) && (DateTime.Now - dicChannelInfo[channelName].LastUpdated).TotalSeconds <= CHANNEL_REFRESH_TIME_SEC)
                {
                    return dicChannelInfo[channelName].Channel;
                }

                if (!TwitchTokenManager.Instance.TokenExists)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "GetChannelInfo called without a valid token");
                    return null;
                }
                var channel = await comm.GetChannelInfo(channelName);
                if (channel == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"GetChannelInfo returned null for channel: {channelName}");
                }

                dicChannelInfo[channelName] = new TwitchChannelUpdateInfo() { Channel = channel, LastUpdated = DateTime.Now };
                return channel;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetChannelInfo Exception: {ex}");
                return null;
            }
            finally
            {
                channelInfoLock.Release();
            }
        }

        public async Task<TwitchActiveStreamer[]> GetActiveStreamers()
        {
            try
            {
                if (!TwitchTokenManager.Instance.TokenExists)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "GetActiveStreamers called without a valid token");
                    return null;
                }

                if ((DateTime.Now - lastActiveStreamers).TotalSeconds >= ACTIVE_STREAMERS_REFRESH_TIME_SEC)
                {
                    activeStreamers = await comm.GetActiveStreamers();
                    lastActiveStreamers = DateTime.Now;
                }

                return activeStreamers;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetActiveStreamers Exception: {ex}");
            }
            return null;
        }

        #endregion
    }
}
