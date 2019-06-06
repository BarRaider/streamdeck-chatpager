using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Twitch
{
    public class TwitchGlobalSettings
    {
        [JsonProperty(PropertyName = "chatMessage")]
        public string ChatMessage { get; set; }

        [JsonProperty(PropertyName = "twoLettersPerKey")]
        public bool TwoLettersPerKey { get; set; }
    }
}
