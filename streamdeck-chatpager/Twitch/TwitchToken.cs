using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatPager.Twitch
{
    [Serializable]
    public class TwitchToken
    {
        [JsonProperty(PropertyName = "token")]
        public string Token { get; set; }

        [JsonIgnore]
        public DateTime TokenLastRefresh { get; set; }
    }
}
