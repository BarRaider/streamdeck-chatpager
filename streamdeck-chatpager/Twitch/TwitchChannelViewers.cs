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
        [JsonProperty(PropertyName = "chatter_count")]
        public int Count { get; private set; }

        [JsonProperty(PropertyName = "chatters")]
        public TwitchChannelViewersTypes Viewers { get; private set; }

        [JsonIgnore]
        public DateTime LastUpdated { get; set; }

        public int TotalViewers
        {
            get
            {
                if (Viewers == null)
                {
                    return 0;
                }

                return AllViewers.Count;
            }
        }

        public List<string> AllViewers
        {
            get
            {
                List<String> viewers = new List<string>();
                if (Viewers == null)
                {
                    return viewers;
                }

                foreach (string user in Viewers.Admin)
                {
                    viewers.Add(user);
                }

                foreach (string user in Viewers.GlobalMods)
                {
                    viewers.Add(user);
                }

                foreach (string user in Viewers.Moderators)
                {
                    viewers.Add(user);
                }

                foreach (string user in Viewers.Staff)
                {
                    viewers.Add(user);
                }

                foreach (string user in Viewers.Viewers)
                {
                    viewers.Add(user);
                }

                foreach (string user in Viewers.VIPs)
                {
                    viewers.Add(user);
                }

                return viewers.Distinct().ToList();
            }
        }
    }
}
