using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Wrappers
{
    public class AdDetails
    {
        [JsonProperty(PropertyName = "retry_after")]
        public int Cooldown { get; private set; }

        [JsonProperty(PropertyName = "length")]
        public int AdLength { get; private set; }

        [JsonProperty(PropertyName = "message")]
        public string ErrorMessage { get; private set; }

        public DateTime NextAdTime { get; private set; } = DateTime.MinValue;

        public DateTime AdEndTime { get; private set; } = DateTime.MinValue;
        public void CalculateAdInfo()
        {
            AdEndTime = DateTime.Now.AddSeconds(AdLength);
            NextAdTime = DateTime.Now.AddSeconds(Cooldown);
        }
    }
}
