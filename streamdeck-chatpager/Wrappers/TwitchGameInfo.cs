using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    public class TwitchGameInfo
    {
        [JsonProperty(PropertyName = "id")]
        public string GameId { get; private set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; private set; }

        [JsonProperty(PropertyName = "box_art_url")]
        public string ImageUrl { get; private set; }

        public Image GameImage { get; set; }
    }
}
