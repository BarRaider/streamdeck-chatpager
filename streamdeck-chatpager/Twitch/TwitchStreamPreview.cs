using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Twitch
{
    public class TwitchStreamPreview
    {
        [JsonProperty(PropertyName = "small")]
        public string Small { get; private set; }

        [JsonProperty(PropertyName = "medium")]
        public string Medium { get; private set; }

        [JsonProperty(PropertyName = "large")]
        public string Large { get; private set; }

        [JsonProperty(PropertyName = "template")]
        public string Template { get; private set; }
    }
}
