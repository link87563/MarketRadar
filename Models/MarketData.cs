using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketRadar.Models
{
    public class MarketData
    {
        public decimal YieldSpread { get; set; }

        public decimal NasdaqChange { get; set; }
        public decimal DollarChange { get; set; }

        public PriceTrend NasdaqTrend { get; set; } = new();
        public PriceTrend DollarTrend { get; set; } = new();
    }
}
