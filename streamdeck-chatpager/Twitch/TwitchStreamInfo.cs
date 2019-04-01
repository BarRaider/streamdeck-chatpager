using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatPager.Twitch
{
    public class TwitchStreamInfo
    {
        [JsonProperty(PropertyName = "game")]
        public string Game { get; private set; }

        [JsonProperty(PropertyName = "viewers")]
        public int Viewers { get; private set; }

        [JsonProperty(PropertyName = "created_at")]
        public DateTime StreamStart { get; private set; }

        [JsonProperty(PropertyName = "stream_type")]
        public string StreamType { get; private set; }

        [JsonProperty(PropertyName = "average_fps")]
        public int FPS { get; private set; }
    }
}
