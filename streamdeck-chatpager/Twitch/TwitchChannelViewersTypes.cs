using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Twitch
{
    public class TwitchChannelViewersTypes
    {
        [JsonProperty(PropertyName = "broadcaster")]
        public string[] Broadcaster { get; private set; }

        [JsonProperty(PropertyName = "vips")]
        public string[] VIPs { get; private set; }

        [JsonProperty(PropertyName = "moderators")]
        public string[] Moderators { get; private set; }

        [JsonProperty(PropertyName = "staff")]
        public string[] Staff { get; private set; }

        [JsonProperty(PropertyName = "admins")]
        public string[] Admin { get; private set; }
        
        [JsonProperty(PropertyName = "global_mods")]
        public string[] GlobalMods { get; private set; }

        [JsonProperty(PropertyName = "viewers")]
        public string[] Viewers { get; private set; }
    }
}
