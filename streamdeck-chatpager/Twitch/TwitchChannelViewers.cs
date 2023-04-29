using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Twitch
{
    public class TwitchChannelViewers
    {
        [JsonProperty(PropertyName = "total")]
        public int Count { get; private set; }

        [JsonProperty(PropertyName = "data")]
        public List<TwitchChannelViewersData> Chatters { get; private set; }

        [JsonIgnore]
        public DateTime LastUpdated { get; set; }

        public List<string> AllViewers
        {
            get
            {
                if (Chatters == null)
                {
                    return new List<string>(); ;
                }

                return Chatters.Select(c => c.Login).Distinct().OrderBy(l => l).ToList();
            }
        }
    }
}
