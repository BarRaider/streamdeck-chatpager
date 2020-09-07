using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    public class TwitchUserInfo
    {
        [JsonProperty(PropertyName = "id")]
        public string UserId { get; private set; }

        [JsonProperty(PropertyName = "login")]
        public string Login { get; private set; }

        [JsonProperty(PropertyName = "display_name")]
        public string Name { get; private set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; private set; }

        [JsonProperty(PropertyName = "broadcaster_type")]
        public string BroadcasterType { get; private set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; private set; }

        [JsonProperty(PropertyName = "profile_image_url")]
        public string ProfileImageUrl { get; private set; }

        [JsonProperty(PropertyName = "offline_image_url")]
        public string OfflineImageUrl { get; private set; }

        [JsonProperty(PropertyName = "view_count")]
        public int ViewCount { get; private set; }
    }
}
