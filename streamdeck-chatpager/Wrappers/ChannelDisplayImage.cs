using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    public enum ChannelDisplayImage
    {
        StreamPreview,
        GameIcon,
        UserIcon
    }

    public static class ChannelDisplayExtensionMethods
    {
        public static ChannelDisplayImage FromSettings(bool streamPreview, bool gameIcon, bool userIcon)
        {
            if (gameIcon)
            {
                return ChannelDisplayImage.GameIcon;
            }
            else if (userIcon)
            {
                return ChannelDisplayImage.UserIcon;
            }
            return ChannelDisplayImage.StreamPreview;
        }
    }
}
