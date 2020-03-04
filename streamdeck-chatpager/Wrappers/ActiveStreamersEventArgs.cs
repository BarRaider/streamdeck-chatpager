using ChatPager.Twitch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    public class ActiveStreamersEventArgs : EventArgs
    {
        public TwitchActiveStreamer [] ActiveStreamers { get; private set; }
        public int NumberOfKeys { get; private set; }
        public int CurrentPage { get; set; }

        public TwitchLiveStreamersLongPressAction LongPressAction { get; private set; }

        public ActiveStreamersEventArgs(TwitchActiveStreamer[] activeStreamers, TwitchLiveStreamersLongPressAction longPressAction, int numberOfKeys, int currentPage)
        {
            ActiveStreamers = activeStreamers;
            NumberOfKeys = numberOfKeys;
            CurrentPage = currentPage;
            LongPressAction = longPressAction;
        }
    }
}
