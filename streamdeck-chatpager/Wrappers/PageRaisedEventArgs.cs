using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    public class PageRaisedEventArgs : EventArgs
    {
        public string Color { get; private set; }
        public string Message { get; private set; }

        public string Author { get; private set; }

        public PageRaisedEventArgs(string author, string message, string color)
        {
            Author = author;
            Message = message;
            Color = color;
        }
    }
}
