using ChatPager.Twitch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    public class TwitchChannelUpdateInfo
    {
        public TwitchChannelInfo Channel { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
