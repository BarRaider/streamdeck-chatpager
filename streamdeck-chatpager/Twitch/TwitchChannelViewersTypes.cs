using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Twitch
{
    public class TwitchChannelViewersData
    {
        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; private set; }

        [JsonProperty(PropertyName = "user_login")]
        public string Login { get; private set; }

        [JsonProperty(PropertyName = "user_name")]
        public string DisplayName { get; private set; }
    }
}
