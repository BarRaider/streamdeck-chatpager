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

        [JsonProperty(PropertyName = "pubsubNotifications")]
        public bool PubsubNotifications { get; set; }

        [JsonProperty(PropertyName = "bitsFlashColor")]
        public string BitsFlashColor { get; set; }

        [JsonProperty(PropertyName = "bitsFlashMessage")]
        public string BitsFlashMessage { get; set; }

        [JsonProperty(PropertyName = "bitsChatMessage")]
        public string BitsChatMessage { get; set; }

        [JsonProperty(PropertyName = "followFlashColor")]
        public string FollowFlashColor { get; set; }

        [JsonProperty(PropertyName = "followFlashMessage")]
        public string FollowFlashMessage { get; set; }

        [JsonProperty(PropertyName = "followChatMessage")]
        public string FollowChatMessage { get; set; }

        [JsonProperty(PropertyName = "subFlashColor")]
        public string SubFlashColor { get; set; }

        [JsonProperty(PropertyName = "subFlashMessage")]
        public string SubFlashMessage { get; set; }

        [JsonProperty(PropertyName = "subChatMessage")]
        public string SubChatMessage { get; set; }

        [JsonProperty(PropertyName = "pointsFlashColor")]
        public string PointsFlashColor { get; set; }

        [JsonProperty(PropertyName = "pointsFlashMessage")]
        public string PointsFlashMessage { get; set; }

        [JsonProperty(PropertyName = "pointsChatMessage")]
        public string PointsChatMessage { get; set; }
    }
}
