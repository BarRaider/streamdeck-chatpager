using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Twitch
{
    public class TwitchActiveStreamer : TwitchStreamInfo
    {
        [JsonProperty(PropertyName = "preview")]
        public TwitchStreamPreview PreviewImages { get; private set; }
        
    }
}
