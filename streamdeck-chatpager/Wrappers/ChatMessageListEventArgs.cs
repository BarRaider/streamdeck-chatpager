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

        public ChatMessageListEventArgs(ChatMessageKey[] chatMessageKeys)
        {
            ChatMessageKeys = chatMessageKeys;
        }
    }
}
