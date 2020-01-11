using ChatPager.Twitch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    public class ChatMessageKey
    {
        public string KeyTitle { get; private set; }

        public string KeyImageURL { get; private set; }

        public string ChatMessage { get; private set; }

        public ChatMessageKey(string keyTitle, string keyImageURL, string chatMessage)
        {
            KeyTitle = keyTitle;
            KeyImageURL = keyImageURL;
            ChatMessage = chatMessage;
        }
    }
}
