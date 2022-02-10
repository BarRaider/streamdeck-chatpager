using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Actions
{
    [PluginActionId("com.barraider.twitchtools.bantimeout")]
    public class TwitchBanTimeoutAction : TwitchShoutoutAction
    {
        private const int DEFAULT_DURATION_SECONDS = 600;
        public TwitchBanTimeoutAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
        }

        protected override void InitializeSettings()
        {
            if (!Int32.TryParse(Settings.Suffix, out int duration))
            {
                Settings.Suffix = DEFAULT_DURATION_SECONDS.ToString();
            }
            else if (duration < 1 || duration > DEFAULT_DURATION_SECONDS)
            {
                Settings.Suffix = DEFAULT_DURATION_SECONDS.ToString();
            }
            
            base.InitializeSettings();
        }

        public override void KeyPressed(KeyPayload payload)
        {
            base.KeyPressed(payload);
        }
    }
}
