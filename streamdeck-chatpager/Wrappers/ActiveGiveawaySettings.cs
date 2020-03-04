using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    internal class ActiveGiveawaySettings
    {
        internal HashSet<string> Entries { get; private set; }
        internal bool IsOpen { get; set; }

        public ActiveGiveawaySettings()
        {
            Entries = new HashSet<string>();
            IsOpen = true;
        }
    }
}
