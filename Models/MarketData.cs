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
        public PriceTrend VixTrend { get; set; } = new();
        public PriceTrend SoxTrend { get; set; } = new();
        public PriceTrend TsmTrend { get; set; } = new();
        public PriceTrend GoldTrend { get; set; } = new();
        public PriceTrend BtcTrend { get; set; } = new();
        public PriceTrend UsdTwdTrend { get; set; } = new();
        public PriceTrend TltTrend { get; set; } = new();
        public PriceTrend HygTrend { get; set; } = new();
        public PriceTrend OilTrend { get; set; } = new();
    }
}
