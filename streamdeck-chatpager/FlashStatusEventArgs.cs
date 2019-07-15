using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager
{
    public class FlashStatusEventArgs : EventArgs
    {
        public Color FlashColor { get; private set; }
        public string FlashMessage { get; private set; }

        public FlashStatusEventArgs(Color flashColor, string flashMessage)
        {
            FlashColor = flashColor;
            FlashMessage = flashMessage;
        }

    }
}
