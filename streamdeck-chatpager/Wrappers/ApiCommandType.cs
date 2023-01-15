using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    internal enum ApiCommandType
    {
        None = 0,
        BanTimeout,
        UnbanUntimeout,
        Announcement,
        Raid,
        Mod,
        Unmod,
        Vip,
        Unvip,
    }
}
