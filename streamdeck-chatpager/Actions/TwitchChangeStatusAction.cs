using BarRaider.SdTools;
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
                    GameFile = String.Empty

                };
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "statusFile")]
            public string StatusFile { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "gameFile")]
            public string GameFile { get; set; }
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
                    if (await comm.UpdateChanelStatus(status, game))
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

        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        #endregion
    }
}
