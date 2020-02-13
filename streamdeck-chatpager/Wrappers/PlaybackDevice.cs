using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    public class PlaybackDevice
    {
        [JsonProperty(PropertyName = "name")]
        public string ProductName {get; set;}
    }
}
