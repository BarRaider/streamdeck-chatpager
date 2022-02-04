using ChatPager.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatPager.Twitch
{
    public class TwitchStreamInfoEventArgs : EventArgs
    {
        public TwitchChannelInfo StreamInfo { get; private set; }

        public TwitchStreamInfoEventArgs(TwitchChannelInfo streamInfo)
        {
            StreamInfo = streamInfo;
        }
    }
}
