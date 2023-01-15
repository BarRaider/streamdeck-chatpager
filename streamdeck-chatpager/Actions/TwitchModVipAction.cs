using BarRaider.SdTools;
using ChatPager.Backend;
using ChatPager.Twitch;
using ChatPager.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Actions
{
    [PluginActionId("com.barraider.twitchtools.modvipcmd")]
    public class TwitchModVipAction : UserSelectionActionBase
    {
        public enum ModVipCommand
        {
            Mod,
            Unmod,
            Vip,
            Unvip
        }
        protected class PluginSettings : UserSelectionPluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    TokenExists = false,
                    Channel = String.Empty,
                    UsersDisplay = UsersDisplay.ActiveChatters,
                    DontLoadImages = false,
                    Command = ModVipCommand.Vip,
                };
                return instance;
            }

            [JsonProperty(PropertyName = "command")]
            public ModVipCommand Command { get; set; }
        }

        protected PluginSettings Settings
        {
            get
            {
                var result = settings as PluginSettings;
                if (result == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} Cannot convert PluginSettingsBase to PluginSettings");
                }
                return result;
            }
            set
            {
                settings = value;
            }
        }

        #region Private Members

        private const int DEFAULT_DURATION_SECONDS = 600;

        #endregion

        public TwitchModVipAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.Settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.Settings = payload.Settings.ToObject<PluginSettings>();
            }

            Settings.TokenExists = TwitchTokenManager.Instance.TokenExists;
            InitializeSettings();
            SaveSettings();
        }

        #region Public Methods

        public override void Dispose()
        {
            base.Dispose();
        }

        public override async void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
            if (!TwitchTokenManager.Instance.TokenExists)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} called without a valid token");
                await Connection.ShowAlert();
                return;
            }
            string[] userNames = await GetUsersList();
            if (userNames == null)
            {
                await Connection.ShowAlert();
                return;
            }

            ApiCommandType apiCommand = ApiCommandType.Unvip;
            switch (Settings.Command)
            {
                case ModVipCommand.Mod:
                    apiCommand = ApiCommandType.Mod;
                    break;
                case ModVipCommand.Unmod:
                    apiCommand = ApiCommandType.Unmod;
                    break;
                case ModVipCommand.Vip:
                    apiCommand = ApiCommandType.Vip;
                    break;
                case ModVipCommand.Unvip:
                    apiCommand = ApiCommandType.Unvip;
                    break;
                default:
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} invallid command {Settings.Command}");
                    return;
            }

            // We have a list of usernames, get some more details on them so we can display their image on the StreamDeck
            List<UserSelectionEventSettings> userSelectionSettings = new List<UserSelectionEventSettings>();
            foreach (string username in userNames)
            {
                TwitchUserInfo userInfo = null;
                if (!Settings.DontLoadImages)
                {
                    userInfo = await TwitchUserInfoManager.Instance.GetUserInfo(username);
                }
                userSelectionSettings.Add(new UserSelectionEventSettings(UserSelectionEventType.ApiCommand, userInfo?.Name ?? username, userInfo?.ProfileImageUrl, userInfo?.UserId ?? null, null, apiCommand));
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPress returned {userSelectionSettings?.Count} users");

            // Show the active chatters on the StreamDeck
            if (userSelectionSettings != null && userSelectionSettings.Count > 0)
            {
                AlertManager.Instance.Initialize(Connection);
                AlertManager.Instance.ShowUserSelectionEvent(userSelectionSettings.ToArray(), Settings.Channel);
            }
            else
            {
                await Connection.ShowOk();
            }
        }
        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick()
        {
            baseHandledOnTick = false;
            base.OnTick();

            if (baseHandledOnTick)
            {
                return;
            }
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(Settings, payload.Settings);
            InitializeSettings();
            SaveSettings();
        }

        #endregion


        #region Private Methods

        protected void InitializeSettings()
        {
        }

        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        #endregion
    }
}
