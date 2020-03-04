using BarRaider.SdTools;
using ChatPager.Backend;
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
                    TagsFile = String.Empty

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

        private const string ALL_TAGS_FILE = "tags.csv";

        private Dictionary<string, string> dicTags;

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

            await UpdateStatus();
            await UpdateTags();
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

        public async Task UpdateStatus()
        {
            try
            {
                string status = String.Empty;
                string game = String.Empty;

                // Read Status from File
                if (!string.IsNullOrEmpty(Settings.StatusFile))
                {
                    if (!File.Exists(Settings.StatusFile))
                    {
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"Status file does not exists: {Settings.StatusFile}");
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
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"Game file does not exists: {Settings.GameFile}");
                    }
                    else
                    {
                        game = File.ReadAllText(Settings.GameFile);
                    }
                }

                if (String.IsNullOrEmpty(status) && String.IsNullOrEmpty(game))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"UpdateStatus called but both status and game are empty");
                    return;
                }

                using (TwitchComm comm = new TwitchComm())
                {
                    if (await comm.UpdateChannelStatus(status, game))
                    {
                        await Connection.ShowOk();
                    }
                    else
                    {
                        await Connection.ShowAlert();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"UpdateStatus Exception: {ex}");
            }
        }

        private async Task UpdateTags()
        {
            try
            {
                if (string.IsNullOrEmpty(Settings.TagsFile))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "UpdateTags called but not tags file set");
                    return;
                }

                if (!File.Exists(Settings.TagsFile))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Tags file does not exists: {Settings.TagsFile}");
                    return;
                }

                string[] tags = File.ReadAllLines(Settings.TagsFile);
                if (tags.Length == 0)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Tags file is empty: {Settings.TagsFile}");
                    return;
                }

                string[] tagIds = GetTagIdsFromTagNames(tags);
                if (tagIds == null)
                {
                    await Connection.ShowAlert();
                }

                if (tagIds.Length > 0)
                {
                    using (TwitchComm comm = new TwitchComm())
                    {
                        if (await comm.UpdateChannelTags(tagIds))
                        {
                            await Connection.ShowOk();
                        }
                        else
                        {
                            await Connection.ShowAlert();
                        }
                    }
                }
                    
            }

            catch (Exception ex) 
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"UpdateTags Exception: {ex}");
            }
        }


        private string[] GetTagIdsFromTagNames(string[] tagNames)
        {
            if (dicTags == null)
            {
                if (!File.Exists(ALL_TAGS_FILE))
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Could not load All Tags File");
                    return null;
                }

                string[] lines = File.ReadAllLines(ALL_TAGS_FILE);
                dicTags = new Dictionary<string, string>();

                foreach (var line in lines)
                {
                    var tag = line.Split(',');
                    if (tag.Length == 2)
                    {
                        dicTags[tag[0].ToLowerInvariant()] = tag[1];
                    }
                    else
                    {
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid Tag Line: {line}");
                    }
                }
            }

            List<string> tagIds = new List<string>();
            foreach (string tagName in tagNames)
            {
                if (!dicTags.ContainsKey(tagName.ToLowerInvariant()))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Tag not found: {tagName}");
                    continue;
                }
                tagIds.Add(dicTags[tagName.ToLowerInvariant()]);
            }

            return tagIds.ToArray();
        }

        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        #endregion
    }
}
