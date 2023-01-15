using ChatPager.Twitch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    internal class UserSelectionEventArgs : EventArgs
    {
        public UserSelectionEventSettings[] KeysDetails { get; private set; }
        public int CurrentPage { get; set; }

        public int NumberOfKeys { get; private set; }

        public string Channel { get; private set; }


        public UserSelectionEventArgs(UserSelectionEventSettings[] keysDetails, string channel, int numberOfKeys, int currentPage)
        {
            KeysDetails = keysDetails;
            NumberOfKeys = numberOfKeys;
            CurrentPage = currentPage;
            Channel = channel;
        }
    }
}
