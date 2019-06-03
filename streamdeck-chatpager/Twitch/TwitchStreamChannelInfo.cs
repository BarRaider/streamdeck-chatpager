using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Twitch
{
    public class TwitchStreamChannelInfo
    {
        [JsonProperty(PropertyName = "status")]
        public string Status { get; private set; }

        [JsonProperty(PropertyName = "mature")]
        public bool IsMature { get; private set; }

        [JsonProperty(PropertyName = "partner")]
        public bool IsPartner { get; private set; }

        [JsonProperty(PropertyName = "followers")]
        public int Followers { get; private set; }

        [JsonProperty(PropertyName = "views")]
        public int Views { get; private set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; private set; }

    }
}
