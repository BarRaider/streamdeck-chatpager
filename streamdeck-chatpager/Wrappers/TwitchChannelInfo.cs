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
        private int? gameId;

        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; private set; }

        [JsonProperty(PropertyName = "user_login")]
        public string UserName { get; private set; }

        [JsonProperty(PropertyName = "user_name")]
        public string UserDisplayName { get; private set; }

        [JsonProperty(PropertyName = "game_id")]
        public string GameIdAsString { get; private set; }

        [JsonProperty(PropertyName = "game_name")]
        public string GameName { get; private set; }

        [JsonProperty(PropertyName = "id")]
        public string StreamId { get; private set; }

        [JsonProperty(PropertyName = "type")]
        public string StreamType { get; private set; }

        [JsonProperty(PropertyName = "title")]
        public string StreamTitle { get; private set; }

        [JsonProperty(PropertyName = "viewer_count")]
        public int Viewers { get; private set; }

        [JsonProperty(PropertyName = "started_at")]
        public DateTime StreamStart { get; private set; }

        [JsonProperty(PropertyName = "language")]
        public string Language { get; private set; }

        [JsonProperty(PropertyName = "thumbnail_url")]
        public string ThumbnailURL { get; private set; }

        public bool IsLive
        {
            get
            {
                return (StreamType != null && StreamType?.ToLowerInvariant() == IS_LIVE_TYPE);
            }
        }

        public int GameId
        {
            get
            {
                if (gameId.HasValue)
                {
                    return gameId.Value;
                }

                if (Int32.TryParse(GameIdAsString, out int parsedGameId))
                {
                    gameId = parsedGameId;
                    return parsedGameId;
                }

                return -1;
            }
        }
    }
}
