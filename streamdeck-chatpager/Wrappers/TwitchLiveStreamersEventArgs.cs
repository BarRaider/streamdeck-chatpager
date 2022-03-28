using ChatPager.Twitch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    public class TwitchLiveStreamersEventArgs : EventArgs
    {
        public TwitchLiveStreamersDisplaySettings DisplaySettings { get; private set; }
        public int NumberOfKeys { get; private set; }
        public int CurrentPage { get; set; }

        public TwitchLiveStreamersLongPressAction LongPressAction { get; private set; }

        public TwitchLiveStreamersEventArgs(TwitchLiveStreamersDisplaySettings settings, int numberOfKeys, int currentPage)
        {
            DisplaySettings = settings;
            NumberOfKeys = numberOfKeys;
            CurrentPage = currentPage;
        }
    }
}
