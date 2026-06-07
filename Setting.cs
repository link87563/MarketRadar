using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketRadar
{
    public class Setting
    {
        public string FredApiKey { get; set; }
        public DiscordSetting DiscordSetting { get; set; }
    }

    public class DiscordSetting
    {
        public string WebhookUrl { get; set; }
    }
}
