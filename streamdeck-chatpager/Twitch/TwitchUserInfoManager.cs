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
    public class TwitchUserInfoManager
    {
        #region Private Members
        private const int USER_REFRESH_TIME_SEC = 60;

        private static TwitchUserInfoManager instance = null;
        private static readonly object objLock = new object();
        private TwitchComm comm;
        private Dictionary<string, TwitchUserUpdateInfo> dicUserInfo = new Dictionary<string, TwitchUserUpdateInfo>();
        private SemaphoreSlim userInfoLock = new SemaphoreSlim(1, 1);

        #endregion

        #region Constructors

        public static TwitchUserInfoManager Instance
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
                        instance = new TwitchUserInfoManager();
                    }
                    return instance;
                }
            }
        }

        private TwitchUserInfoManager()
        {
            comm = new TwitchComm();
        }

        public async Task<TwitchUserInfo> GetUserInfo(string userName)
        {
            if (String.IsNullOrEmpty(userName))
            {
                return null;
            }

            await userInfoLock.WaitAsync();
            try
            {
                userName = userName.ToLowerInvariant();
                // Check if the information we have about a channel is current
                if (dicUserInfo.ContainsKey(userName) && (DateTime.Now - dicUserInfo[userName].LastUpdated).TotalSeconds <= USER_REFRESH_TIME_SEC)
                {
                    return dicUserInfo[userName].User;
                }

                if (!TwitchTokenManager.Instance.TokenExists)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "GetUserInfo called without a valid token");
                    return null;
                }
                var user = await comm.GetUserInfo(userName);
                if (user == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetUserInfo returned null for user: {userName}");
                }

                dicUserInfo[userName] = new TwitchUserUpdateInfo() { User = user, LastUpdated = DateTime.Now };
                return user;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetUserInfo Exception: {ex}");
                return null;
            }
            finally
            {
                userInfoLock.Release();
            }
        }


        #endregion
    }
}
