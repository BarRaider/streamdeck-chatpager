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

        public PageRaisedEventArgs(string message, string color)
        {
            Message = message;
            Color = color;
        }
    }
}
