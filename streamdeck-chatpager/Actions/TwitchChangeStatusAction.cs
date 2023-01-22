using BarRaider.SdTools;
using ChatPager.Twitch;
using ChatPager.Wrappers;
using HtmlAgilityPack;
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
using static System.Windows.Forms.LinkLabel;

namespace ChatPager.Actions
{

    //---------------------------------------------------
    //          BarRaider's Hall Of Fame
    // 100 Bits: Vedeksu
    // Subscriber: ASSASSIN0831
    //---------------------------------------------------

    [PluginActionId("com.barraider.twitchtools.streamtitle")]
    public class TwitchChangeStatusAction : ActionBase
    {
        protected class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    TokenExists = false,
                    Version = CURRENT_VERSION,
                    LoadFromFile = false,
                    Status = String.Empty,
                    StatusFile = String.Empty,
                    Game = String.Empty,
                    GameFile = String.Empty,
                    Tags = String.Empty,
                    TagsFile = String.Empty,
                    Language = String.Empty,
                    LanguageFile = String.Empty
                };
                return instance;
            }

            [JsonProperty(PropertyName = "version")]
            public int Version { get; set; }

            [JsonProperty(PropertyName = "loadFromFile")]
            public bool LoadFromFile { get; set; }

            [JsonProperty(PropertyName = "status")]
            public string Status { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "statusFile")]
            public string StatusFile { get; set; }

            [JsonProperty(PropertyName = "game")]
            public string Game { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "gameFile")]
            public string GameFile { get; set; }

            [JsonProperty(PropertyName = "tags")]
            public string Tags { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "tagsFile")]
            public string TagsFile { get; set; }

            [JsonProperty(PropertyName = "language")]
            public string Language { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "languageFile")]
            public string LanguageFile { get; set; }
        }

        protected PluginSettings Settings
        {
            get
            {
                var result = settings as PluginSettings;
                if (result == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot convert PluginSettingsBase to PluginSettings");
                }
                return result;
            }
            set
            {
                settings = value;
            }
        }

        #region Private Members

        private const int CURRENT_VERSION = 1;

        #endregion


        #region Public Methods

        public TwitchChangeStatusAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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

        public override void Dispose() { }

        public override async void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPressed");
            if (!TwitchTokenManager.Instance.TokenExists)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} called without a valid token");
                await Connection.ShowAlert();
                return;
            }

            // Added as booleans otherwise it will short circuit the second call
            bool statusResult = await UpdateStatus();
            //bool tagsResult = await UpdateTags();

            //if (statusResult || tagsResult)
            if (statusResult)
            {
                await Connection.ShowOk();
            }
            else
            {
                await Connection.ShowAlert();
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

        public async Task<bool> UpdateStatus()
        {
            try
            {
                string status = GetStatus();
                string game = GetGame(); ;
                string language = GetLanguage();
                int? gameId = null;

                if (String.IsNullOrEmpty(status) && String.IsNullOrEmpty(game) && String.IsNullOrEmpty(language))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"UpdateStatus called but status, game and language are all empty");
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(game))
                {
                    gameId = await TwitchChannelInfoManager.Instance.GetGameId(game);
                }

                using (TwitchComm comm = new TwitchComm())
                {
                    return await comm.UpdateChannelStatus(status, gameId, language);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"UpdateStatus Exception: {ex}");
            }
            return false;
        }

        private string GetStatus()
        {
            string[] lines = null;
            if (!Settings.LoadFromFile)
            {
                lines = Settings.Status.Replace("\r", "").Split('\n');
            }
            else if (!string.IsNullOrEmpty(Settings.StatusFile)) // Read Status from File
            {
                if (!File.Exists(Settings.StatusFile))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Status file does not exists: {Settings.StatusFile}");
                    return null;
                }
                else
                {
                    lines = File.ReadAllLines(Settings.StatusFile);
                }
            }

            if (lines != null)
            {
                if (lines.Length == 1)
                {
                    return lines[0];
                }

                if (lines.Length > 1) // There are multiple lines in the file, choose a random one.
                {
                    int retries = 0;
                    string status;
                    do
                    {
                        // Choose a random one
                        status = lines[RandomGenerator.Next(lines.Length)];
                        retries++;
                    } while (String.IsNullOrEmpty(status) && retries < 5);
                    return status;
                }
            }
            return null;
        }

        private string GetGame()
        {
            if (!Settings.LoadFromFile)
            {
                return Settings.Game;
            }

            // Read Game from File
            if (!string.IsNullOrEmpty(Settings.GameFile))
            {
                if (!File.Exists(Settings.GameFile))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Game file does not exists: {Settings.GameFile}");
                }
                else
                {
                    return File.ReadAllText(Settings.GameFile);
                }
            }
            return null;
        }

        private string GetLanguage()
        {
            if (!Settings.LoadFromFile)
            {
                return Settings.Language;
            }


            // Read Language from File
            if (!string.IsNullOrEmpty(Settings.LanguageFile))
            {
                if (!File.Exists(Settings.LanguageFile))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Language file does not exists: {Settings.LanguageFile}");
                }
                else
                {
                    return File.ReadAllText(Settings.LanguageFile);
                }
            }
            return null;
        }

        private string[] GetTags()
        {
            try
            {
                if (!Settings.LoadFromFile)
                {
                    return Settings.Tags.Replace("\r", "").Split('\n');
                }

                if (string.IsNullOrEmpty(Settings.TagsFile))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "UpdateTags called but not tags file set");
                    return null;
                }

                if (!File.Exists(Settings.TagsFile))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Tags file does not exists: {Settings.TagsFile}");
                    return null;
                }

                return File.ReadAllLines(Settings.TagsFile);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} GetTags Exception: {ex}");
            }

            return null;
        }

        private async Task<bool> UpdateTags()
        {
            try
            {
                string[] tags = GetTags();
                if (tags == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"GetTags returned null!");
                    return false;
                }

                if (tags.Length == 0)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Tags is empty. LoadFromFile: {Settings.LoadFromFile} {Settings.TagsFile}");
                    return false;
                }

                string[] tagIds = await TwitchTagsManager.Instance.GetTagIdsFromTagNames(tags);
                if (tagIds == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} GetTagIdsFromTagNames failed!");
                    return false;
                }

                if (tagIds.Length > 0)
                {
                    using (TwitchComm comm = new TwitchComm())
                    {
                        return await comm.UpdateChannelTags(tagIds);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"UpdateTags Exception: {ex}");
            }
            return false;
        }

        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        private void InitializeSettings()
        {
        }

        #endregion
    }
}
