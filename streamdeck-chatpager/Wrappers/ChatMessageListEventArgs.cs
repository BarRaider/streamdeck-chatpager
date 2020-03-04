using ChatPager.Twitch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    public class ChatMessageListEventArgs : EventArgs
    {
        public ChatMessageKey[] ChatMessageKeys { get; private set; }
        public int CurrentPage { get; set; }

        public int NumberOfKeys { get; private set; }

        public string Channel { get; private set; }


        public ChatMessageListEventArgs(ChatMessageKey[] chatMessageKeys, string channel, int numberOfKeys, int currentPage)
        {
            ChatMessageKeys = chatMessageKeys;
            NumberOfKeys = numberOfKeys;
            CurrentPage = currentPage;
            Channel = channel;
        }
    }
}
