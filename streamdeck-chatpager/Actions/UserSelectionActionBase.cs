using BarRaider.SdTools;
using ChatPager.Backend;
using ChatPager.Twitch;
using ChatPager.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Actions
{

    //---------------------------------------------------
    //          BarRaider's Hall Of Fame
    // 100 Bits: Vedeksu
    //---------------------------------------------------

    public abstract class UserSelectionActionBase : ActionBase
    {
        public enum UsersDisplay
        {
            ActiveChatters,
            AllViewers
        }
        protected abstract class UserSelectionPluginSettings : PluginSettingsBase
        {
            [JsonProperty(PropertyName = "channel")]
            public string Channel { get; set; }

            [JsonProperty(PropertyName = "usersDisplay")]
            public UsersDisplay UsersDisplay { get; set; }

            [JsonProperty(PropertyName = "dontLoadImages")]
            public bool DontLoadImages { get; set; }
        }

        private UserSelectionPluginSettings Settings
        {
            get
            {
                var result = settings as UserSelectionPluginSettings;
                if (result == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot convert PluginSettingsBase to UserSelectionPluginSettings");
                }
                return result;
            }
            set
            {
                settings = value;
            }
        }

        #region Public Methods

        public UserSelectionActionBase(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            TwitchChat.Instance.Initialize();
        }

        protected async Task<string[]> GetUsersList()
        {
            // Show Active Chatters
            if (Settings.UsersDisplay == UsersDisplay.ActiveChatters)
            {
                var chatters = TwitchChat.Instance.GetLastChatters();
                if (chatters == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "GetLastChatters returned null");
                    return null;
                }

                Logger.Instance.LogMessage(TracingLevel.INFO, "Shoutout loading active chatters");
                return chatters.ToArray();
            }
            else // Show all viewers
            {
                string channel = Settings.Channel;
                if (String.IsNullOrEmpty(channel))
                {
                    channel = TwitchTokenManager.Instance.User.UserName;
                }
                var viewers = await TwitchChannelInfoManager.Instance.GetChannelViewers(channel);
                if (viewers == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "GetChannelViewers returned null");
                    return null;
                }
                Logger.Instance.LogMessage(TracingLevel.INFO, "Shoutout loading all viewers");
                return viewers.AllViewers.OrderBy(u => u.ToLowerInvariant()).ToArray();
            }
        }

        #endregion
    }
}
