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

        public ActiveStreamersEventArgs(TwitchActiveStreamer[] activeStreamers)
        {
            ActiveStreamers = activeStreamers;
        }
    }
}
