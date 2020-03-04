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
    // Subscriber: ChessCoachNet - Gifted Sub
    // Subscriber: Sm0ozle - Gifted Sub
    // tobitege - Tip: $20.02
    //---------------------------------------------------
    public class TwitchGiveawayManager
    {
        #region Private Members
        private static TwitchGiveawayManager instance = null;
        private static readonly object objLock = new object();

        private readonly Dictionary<string, ActiveGiveawaySettings> dicActiveGiveaways;

        #endregion

        #region Constructors

        public static TwitchGiveawayManager Instance
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
                        instance = new TwitchGiveawayManager();
                    }
                    return instance;
                }
            }
        }

        private TwitchGiveawayManager()
        {
            dicActiveGiveaways = new Dictionary<string, ActiveGiveawaySettings>();
            TwitchChat.Instance.OnChatMessageReceived += TwitchChat_OnChatMessageReceived;
        }

        #endregion

        #region Public Methods

        public void StartGiveaway(string giveawayCommand)
        {
            dicActiveGiveaways[giveawayCommand.ToLowerInvariant()] = new ActiveGiveawaySettings();
        }

        public void StopGiveaway(string giveawayCommand)
        {
            giveawayCommand = giveawayCommand.ToLowerInvariant();
            if (dicActiveGiveaways.ContainsKey(giveawayCommand))
            {
                dicActiveGiveaways.Remove(giveawayCommand);
            }
        }

        public List<string> GetGiveawayUsers(string giveawayCommand)
        {
            giveawayCommand = giveawayCommand.ToLowerInvariant();
            if (!dicActiveGiveaways.ContainsKey(giveawayCommand))
            {
                return null;
            }

            return dicActiveGiveaways[giveawayCommand].Entries.ToList();
        }

        public void PauseGiveaway(string giveawayCommand)
        {
            giveawayCommand = giveawayCommand.ToLowerInvariant();
            if (dicActiveGiveaways.ContainsKey(giveawayCommand))
            {
                dicActiveGiveaways[giveawayCommand].IsOpen = false;
            }
        }

        public void RemoveGiveawayUser(string giveawayCommand, string user)
        {
            giveawayCommand = giveawayCommand.ToLowerInvariant();
            if (dicActiveGiveaways.ContainsKey(giveawayCommand))
            {
                dicActiveGiveaways[giveawayCommand].Entries.Remove(user);
            }
        }

        public bool IsGiveawayActive(string giveawayCommand)
        {
            return dicActiveGiveaways.ContainsKey(giveawayCommand.ToLowerInvariant());
        }

        public bool IsGiveawayOpen(string giveawayCommand)
        {
            giveawayCommand = giveawayCommand.ToLowerInvariant();
            if (dicActiveGiveaways.ContainsKey(giveawayCommand))
            {
                return dicActiveGiveaways[giveawayCommand].IsOpen;
            }
            return false;
        }

        #endregion

        #region Private Methods

        private void TwitchChat_OnChatMessageReceived(object sender, ChatMessageReceivedEventArgs e)
        {
            if (e == null || String.IsNullOrEmpty(e.Message))
            {
                return;
            }

            // Get first word in message, check if it's a giveaway command
            try
            {
                string[] words = e.Message.Split(' ');
                string firstWord = words[0].ToLowerInvariant();
                // If first word is a "giveaway" command, add user to the list (if not already there)
                if (dicActiveGiveaways.ContainsKey(firstWord) && dicActiveGiveaways[firstWord].IsOpen)
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"User {e.Author} has been added to Giveaway {firstWord}");
                    dicActiveGiveaways[firstWord].Entries.Add(e.Author);
                }

                // TODO: Add support for giveaway commands coming from chat (for mods)
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error parsing chat message for Giveaway: {e.Message} {ex}");
            }

        }

        #endregion

    }
}
