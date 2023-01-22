using ChatPager.Twitch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    internal class UserSelectionEventSettings
    {
        public string KeyTitle { get; private set; }

        public string KeyImageURL { get; private set; }

        public string ChatMessage { get; private set; }

        public string UserId { get; private set; }

        public UserSelectionEventType EventType { get; private set; }

        public ApiCommandType ApiCommand { get; private set; }

        public UserSelectionEventSettings(UserSelectionEventType eventType, string keyTitle, string keyImageURL, string userId = null, string chatMessage = null, ApiCommandType commandType = ApiCommandType.None)
        {
            EventType = eventType;
            KeyTitle = keyTitle;
            KeyImageURL = keyImageURL;
            ChatMessage = chatMessage;
            UserId = userId;
            ApiCommand = commandType;
        }
    }
}
