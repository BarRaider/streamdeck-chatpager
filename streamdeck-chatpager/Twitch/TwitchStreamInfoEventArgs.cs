using System;
using System.Collections.Generic;
using System.Text;

namespace ChatPager.Twitch
{
    public class TwitchStreamInfoEventArgs : EventArgs
    {
        public TwitchStreamInfo StreamInfo { get; private set; }

        public TwitchStreamInfoEventArgs(TwitchStreamInfo streamInfo)
        {
            StreamInfo = streamInfo;
        }
    }
}
