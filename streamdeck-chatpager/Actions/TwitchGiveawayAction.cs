using BarRaider.SdTools;
using ChatPager.Twitch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ChatPager.Actions
{

    //---------------------------------------------------
    //          BarRaider's Hall Of Fame
    // Subscriber: ChessCoachNet x3 Gift Subs
    //---------------------------------------------------
    [PluginActionId("com.barraider.twitchtools.giveaway")]
    public class TwitchGiveawayAction : ActionBase
    {
        protected class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    TokenExists = false,
                    Command = DEFAULT_COMMAND,
                    Item = string.Empty,
                    PauseAfterFirstDraw = false,
                    WinnersFileName = string.Empty,
                    AutoDraw = false,
                    TimerInterval = DEFAULT_TIMER_INTERVAL,
                    TimerFileName = String.Empty,
                    Reminder = DEFAULT_REMINDER_MINUTES.ToString(),
                    StartMessage = DEFAULT_GIVEAWAY_START_MESSAGE,
                    WinMessage = DEFAULT_GIVEAWAY_WIN_MESSAGE,
                    ReminderMessage = DEFAULT_GIVEAWAY_REMINDER_MESSAGE,
                    WinnersFileOverwrite = false
                };
                return instance;
            }

            [JsonProperty(PropertyName = "command")]
            public string Command { get; set; }

            [JsonProperty(PropertyName = "item")]
            public string Item { get; set; }

            [JsonProperty(PropertyName = "pauseAfterFirstDraw")]
            public bool PauseAfterFirstDraw { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "winnersFileName")]
            public string WinnersFileName { get; set; }

            [JsonProperty(PropertyName = "autoDraw")]
            public bool AutoDraw { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "timerFileName")]
            public string TimerFileName { get; set; }

            [JsonProperty(PropertyName = "timerInterval")]
            public string TimerInterval { get; set; }

            [JsonProperty(PropertyName = "reminder")]
            public string Reminder { get; set; }

            [JsonProperty(PropertyName = "startMessage")]
            public string StartMessage { get; set; }

            [JsonProperty(PropertyName = "winMessage")]
            public string WinMessage { get; set; }

            [JsonProperty(PropertyName = "reminderMessage")]
            public string ReminderMessage { get; set; }

            [JsonProperty(PropertyName = "winnersFileOverwrite")]
            public bool WinnersFileOverwrite { get; set; }
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

        private const string DEFAULT_COMMAND = "!giveaway";
        private const string DEFAULT_TIMER_INTERVAL = "00:01:00";
        private const string DEFAULT_GIVEAWAY_START_MESSAGE = "New giveaway started for {ITEM} to enter type: {COMMAND} in the channel!";
        private const string DEFAULT_GIVEAWAY_WIN_MESSAGE = "Congratulations @{USER} you won the giveaway for {ITEM}!";
        private const string DEFAULT_GIVEAWAY_REMINDER_MESSAGE = "Giveaway for {ITEM} is still active! To enter type: {COMMAND} in the channel!";
        private const int DEFAULT_REMINDER_MINUTES = 1;
        private const int LONG_KEYPRESS_LENGTH_MS = 600;
        
        private HashSet<string> previousWinners = new HashSet<string>();
        private TimeSpan timerInterval = TimeSpan.MinValue;
        private DateTime giveawayEndTime = DateTime.MaxValue;
        private bool keyPressed = false;
        private bool longKeyPressed = false;
        private DateTime keyPressStart;
        private bool startedAutoDraw = false;
        private DateTime lastReminderTime;
        private int reminderMinutes;

        #endregion

        #region Public Methods

        public TwitchGiveawayAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.Settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.Settings = payload.Settings.ToObject<PluginSettings>();
            }
            Connection.OnSendToPlugin += Connection_OnSendToPlugin;


            Settings.TokenExists = TwitchTokenManager.Instance.TokenExists;
            TwitchChat.Instance.Initialize();
            InitializeSettings();
            SaveSettings();
        }
        public override void Dispose() 
        {
            Connection.OnSendToPlugin -= Connection_OnSendToPlugin;
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
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

            keyPressed = true;
            longKeyPressed = false;
            keyPressStart = DateTime.Now;
        }

        public override void KeyReleased(KeyPayload payload) 
        {
            keyPressed = false;
            if (longKeyPressed) // Take care of the short keypress
            {
                return;
            }

            // Handle Short Keypress
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} Short Keypress");
            if (TwitchGiveawayManager.Instance.IsGiveawayActive(Settings.Command))
            {
                DrawWinner();
            }
            else // Not in active giveaway
            {
                StartNewGiveaway();
            }
        }

        public async override void OnTick()
        {
            baseHandledOnTick = false;
            base.OnTick();

            if (baseHandledOnTick)
            {
                return;
            }

            if (keyPressed && !longKeyPressed)
            {
                int timeKeyWasPressed = (int)(DateTime.Now - keyPressStart).TotalMilliseconds;
                if (timeKeyWasPressed >= LONG_KEYPRESS_LENGTH_MS)
                {
                    await HandleLongKeyPress();
                }
            }

            if (TwitchGiveawayManager.Instance.IsGiveawayActive(Settings.Command))
            {
                var entries = TwitchGiveawayManager.Instance.GetGiveawayUsers(Settings.Command);
                if (entries == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "GetGiveawayUsers returned null entries");
                }
                string title = $"{entries?.Count}\nentries";

                if (Settings.AutoDraw && !startedAutoDraw)
                {
                    int secondsLeft = (int) (giveawayEndTime - DateTime.Now).TotalSeconds;

                    if (secondsLeft >= 0)
                    {
                        long minutes, seconds;
                        minutes = secondsLeft / 60;
                        seconds = secondsLeft % 60;
                        minutes %= 60;

                        string timeRemaining = $"{minutes:00}:{ seconds:00}";
                        title += $"\n{timeRemaining}";
                        HandleTimerFile(timeRemaining);
                    }
                    if (secondsLeft <= 0 && !startedAutoDraw)
                    {
                        startedAutoDraw = true;
                        HandleAutoDraw();
                    }
                }
                HandleReminder();

                await Connection.SetTitleAsync(title);
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
        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        private void StartNewGiveaway()
        {
            TwitchChat.Instance.SendMessage(Settings.StartMessage.Replace("{ITEM}", Settings.Item).Replace("{COMMAND}", Settings.Command));
            previousWinners = new HashSet<string>();
            lastReminderTime = DateTime.Now;
            TwitchGiveawayManager.Instance.StartGiveaway(Settings.Command);

            if (Settings.AutoDraw)
            {
                startedAutoDraw = false;
                if (timerInterval == TimeSpan.MinValue)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "AutoDraw is enabled but invalid timerInterval");
                }

                giveawayEndTime = DateTime.Now + timerInterval;
            }
        }

        private void DrawWinner()
        {
            if (Settings.PauseAfterFirstDraw)
            {
                TwitchGiveawayManager.Instance.PauseGiveaway(Settings.Command);
            }

            var entries = TwitchGiveawayManager.Instance.GetGiveawayUsers(Settings.Command);
            if (entries == null)
            {
                return;
            }

            int maxRetries = 100;
            string winner;
            // Keep selecting someone else, until you have someone who wasn't choosen before
            do
            {
                winner = entries[RandomGenerator.Next(entries.Count)];
                maxRetries--;
            } while (previousWinners.Contains(winner) && maxRetries > 0);

            if (maxRetries <= 0)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Could not Draw Winner, only found previous winners");
                return;
            }


            TwitchChat.Instance.SendMessage(Settings.WinMessage.Replace("{USER}",winner).Replace("{ITEM}", Settings.Item));
            previousWinners.Add(winner);

            SaveWinnerToFile(winner);
            HandleTimerFile($"Winner: {winner}");
        }

        private async void Connection_OnSendToPlugin(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.SendToPlugin> e)
        {
            var payload = e.Event.Payload;

            Logger.Instance.LogMessage(TracingLevel.INFO, "OnSendToPlugin called");
            if (payload["property_inspector"] != null)
            {
                switch (payload["property_inspector"].ToString().ToLowerInvariant())
                {
                    case "loadsavepicker":
                        string propertyName = (string)payload["property_name"];
                        string pickerTitle = (string)payload["picker_title"];
                        string pickerFilter = (string)payload["picker_filter"];
                        string fileName = PickersUtil.Pickers.SaveFilePicker(pickerTitle, null, pickerFilter);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            if (!PickersUtil.Pickers.SetJsonPropertyValue(Settings, propertyName, fileName))
                            {
                                Logger.Instance.LogMessage(TracingLevel.ERROR, "Failed to save picker value to settings");
                            }
                            await SaveSettings();
                        }
                        break;
                    case "choosewinner":
                        DrawWinner();
                        break;
                    case "endgiveaway":
                        await EndGiveaway();
                        break;
                }
            }
        }

        private void SaveWinnerToFile(string winner)
        {
            if (string.IsNullOrEmpty(Settings.WinnersFileName))
            {
                return;
            }

            string contents = $"{DateTime.Now:yyyy-MM-dd HH:mm}, Item: {Settings.Item}, Winner: {winner}\n";
            if (Settings.WinnersFileOverwrite)
            {
                File.WriteAllText(Settings.WinnersFileName, contents);
            }
            else
            {
                File.AppendAllText(Settings.WinnersFileName, contents);
            }
        }

        private async Task EndGiveaway()
        {
            TwitchGiveawayManager.Instance.StopGiveaway(Settings.Command);
            await Connection.SetTitleAsync((String)null);
            HandleTimerFile(String.Empty);
        }

        private async Task HandleLongKeyPress()
        {
            longKeyPressed = true;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} Long Keypress");
            await EndGiveaway();
        }

        private void InitializeSettings()
        {
            SetTimerInterval();
            bool hasChanges = false;
            if (!int.TryParse(Settings.Reminder, out reminderMinutes))
            {
                Settings.Reminder = DEFAULT_REMINDER_MINUTES.ToString();
                hasChanges = true;
            }

            if (String.IsNullOrEmpty(Settings.StartMessage))
            {
                Settings.StartMessage = DEFAULT_GIVEAWAY_START_MESSAGE;
                hasChanges = true;
            }

            if (String.IsNullOrEmpty(Settings.WinMessage))
            {
                Settings.WinMessage = DEFAULT_GIVEAWAY_WIN_MESSAGE;
                hasChanges = true;
            }

            if (String.IsNullOrEmpty(Settings.ReminderMessage))
            {
                Settings.ReminderMessage = DEFAULT_GIVEAWAY_REMINDER_MESSAGE;
                hasChanges = true;
            }

            if (hasChanges)
            {
                SaveSettings();
            }
        }

        private void SetTimerInterval()
        {
            timerInterval = TimeSpan.Zero;
            if (!String.IsNullOrEmpty(Settings.TimerInterval))
            {
                if (!TimeSpan.TryParse(Settings.TimerInterval, out timerInterval))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid Timer Interval: {Settings.TimerInterval}");
                    Settings.TimerInterval = DEFAULT_TIMER_INTERVAL;
                    SaveSettings();
                }
            }
        }

        private void HandleAutoDraw()
        {
            DrawWinner();
        }

        private void HandleTimerFile(string text)
        {
            if (string.IsNullOrEmpty(Settings.TimerFileName))
            {
                return;
            }

            File.WriteAllText(Settings.TimerFileName, text);
        }

        private void HandleReminder()
        {
            if (reminderMinutes <= 0)
            {
                return;
            }

            if (!TwitchGiveawayManager.Instance.IsGiveawayOpen(Settings.Command))
            {
                return;
            }

            if ((DateTime.Now - lastReminderTime).TotalMinutes >= reminderMinutes)
            {
                lastReminderTime = DateTime.Now;
                TwitchChat.Instance.SendMessage(Settings.ReminderMessage.Replace("{ITEM}", Settings.Item).Replace("{COMMAND}", Settings.Command));
            }
        }

        #endregion
    }
}
