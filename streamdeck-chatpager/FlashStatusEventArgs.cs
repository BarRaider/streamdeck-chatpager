using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager
{
    public class FlashStatusEventArgs : EventArgs
    {
        public int FlashIndex { get; private set; }
        public string FlashMessage { get; private set; }

        public FlashStatusEventArgs(int flashIndex, string flashMessage)
        {
            FlashIndex = flashIndex;
            FlashMessage = flashMessage;
        }

    }
}
