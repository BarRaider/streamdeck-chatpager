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
                    StatusFile = String.Empty,
                    GameFile = String.Empty,
                    TagsFile = String.Empty,
                    LanaguageFile = String.Empty
                };
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "statusFile")]
            public string StatusFile { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "gameFile")]
            public string GameFile { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "tagsFile")]
            public string TagsFile { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "languageFile")]
            public string LanaguageFile { get; set; }
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
            bool statusResult = false;
            if (!String.IsNullOrEmpty(Settings.GameFile) || !String.IsNullOrEmpty(Settings.StatusFile) || !String.IsNullOrEmpty(Settings.LanaguageFile))
            {
                statusResult = await UpdateStatus();
            }
            bool tagsResult = await UpdateTags();

            if (statusResult || tagsResult)
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
            SaveSettings();
        }

        #endregion

        #region Private Methods

        public async Task<bool> UpdateStatus()
        {
            try
            {
                string status = null;
                string game = null;
                string language = null;

                // Read Status from File
                if (!string.IsNullOrEmpty(Settings.StatusFile))
                {
                    if (!File.Exists(Settings.StatusFile))
                    {
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"Title file does not exists: {Settings.StatusFile}");
                    }
                    else
                    {
                        string[] lines = File.ReadAllLines(Settings.StatusFile);
                        if (lines.Length > 1) // There are multiple lines in the file, choose a random one.
                        {
                            int retries = 0;
                            do
                            {
                                // Choose a random one
                                status = lines[RandomGenerator.Next(lines.Length)];
                                retries++;
                            } while (String.IsNullOrEmpty(status) && retries < 5);
                        }
                        else if (lines.Length == 1)
                        {
                            status = lines[0];
                        }
                    }
                }

                // Read Game from File
                if (!string.IsNullOrEmpty(Settings.GameFile))
                {
                    if (!File.Exists(Settings.GameFile))
                    {
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"Category file does not exists: {Settings.GameFile}");
                    }
                    else
                    {
                        game = File.ReadAllText(Settings.GameFile);
                    }
                }

                // Read Language from File
                if (!string.IsNullOrEmpty(Settings.LanaguageFile))
                {
                    if (!File.Exists(Settings.LanaguageFile))
                    {
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"Language file does not exists: {Settings.LanaguageFile}");
                    }
                    else
                    {
                        language = File.ReadAllText(Settings.LanaguageFile);
                    }
                }

                if (String.IsNullOrEmpty(status) && String.IsNullOrEmpty(game) && String.IsNullOrEmpty(language))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"UpdateStatus called but status, game and language are all empty");
                    return false;
                }

                using (TwitchComm comm = new TwitchComm())
                {
                    return await comm.UpdateChannelStatus(status, game, language);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"UpdateStatus Exception: {ex}");
            }
            return false;
        }

        private async Task<bool> UpdateTags()
        {
            try
            {
                if (string.IsNullOrEmpty(Settings.TagsFile))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "UpdateTags called but not tags file set");
                    return false;
                }

                if (!File.Exists(Settings.TagsFile))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Tags file does not exists: {Settings.TagsFile}");
                    return false;
                }

                string[] tags = File.ReadAllLines(Settings.TagsFile);
                if (tags.Length == 0)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Tags file is empty: {Settings.TagsFile}");
                    return false;
                }

                string[] tagIds = await TagsManager.Instance.GetTagIdsFromTagNames(tags);
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

        #endregion
    }
}
