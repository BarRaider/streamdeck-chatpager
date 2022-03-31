using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    public class ChatMessageReceivedEventArgs : EventArgs
    {
        public string Message { get; private set; }
        public string Author { get; private set; }

        public string Channel { get; private set; }

        public ChatMessageReceivedEventArgs(string channel, string message, string author)
        {
            Channel = channel;
            Message = message;
            Author = author;
        }
    }
}
