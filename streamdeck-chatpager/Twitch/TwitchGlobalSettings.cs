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

        [JsonProperty(PropertyName = "pageCommand")]
        public string PageCommand { get; set; }

        [JsonProperty(PropertyName = "fullScreenAlert")]
        public bool FullScreenAlert { get; set; }

        [JsonProperty(PropertyName = "twoLettersPerKey")]
        public bool TwoLettersPerKey { get; set; }

        [JsonProperty(PropertyName = "initialAlertColor")]
        public string InitialAlertColor { get; set; }

        [JsonProperty(PropertyName = "saveToFile")]
        public bool SaveToFile { get; set; }

        [JsonProperty(PropertyName = "pageFileName")]
        public string PageFileName { get; set; }

        [JsonProperty(PropertyName = "filePrefix")]
        public string FilePrefix { get; set; }

        [JsonProperty(PropertyName = "clearFileSeconds")]
        public string ClearFileSeconds { get; set; }

        [JsonProperty(PropertyName = "alwaysAlert")]
        public bool AlwaysAlert { get; set; }

        [JsonProperty(PropertyName = "previousViewersCount")]
        public int PreviousViewersCount { get; set; }

        [JsonProperty(PropertyName = "viewersBrush")]
        public string ViewersBrush { get; set; }

        [JsonProperty(PropertyName = "token")]
        public TwitchToken Token { get; set; }
    }
}
