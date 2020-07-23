using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    public class TwitchChannelInfo
    {
        private const string IS_LIVE_TYPE = "live";

        [JsonProperty(PropertyName = "id")]
        public string ChannelId { get; private set; }

        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; private set; }

        [JsonProperty(PropertyName = "user_name")]
        public string UserName { get; private set; }

        [JsonProperty(PropertyName = "game_id")]
        public string GameId { get; private set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; private set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; private set; }

        [JsonProperty(PropertyName = "viewer_count")]
        public int Viewers { get; private set; }

        [JsonProperty(PropertyName = "started_at")]
        public DateTime Started { get; private set; }

        [JsonProperty(PropertyName = "language")]
        public string Language { get; private set; }

        [JsonProperty(PropertyName = "thumbnail_url")]
        public string ThumbnailUrl { get; private set; }

        public bool IsLive
        {
            get
            {
                return (Type != null && Type?.ToLowerInvariant() == IS_LIVE_TYPE);
            }
        }
    }
}
