using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    public class TwitchLiveStreamersDisplaySettings
    {
        public TwitchChannelInfo[] Streamers { get; private set; }

        public TwitchLiveStreamersLongPressAction LongPressAction { get; private set; }

        public ChannelDisplayImage DisplayImage { get; private set; }

        public TwitchLiveStreamersDisplaySettings(TwitchChannelInfo[] streamers, TwitchLiveStreamersLongPressAction longPressAction, ChannelDisplayImage displayImage)
        {
            Streamers = streamers;
            LongPressAction = longPressAction;
            DisplayImage = displayImage;
        }
    }
}
