using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketRadar.Models
{
    public class RiskReport
    {
        public int Score { get; set; }
        public int PreviousScore { get; set; }
        public int ScoreChange { get; set; }
        public string ChangeLabel { get; set; }
        public string Regime { get; set; }
        public string Stress { get; set; }
        public string RiskLabel { get; set; }

        public decimal Nasdaq { get; set; }
        public decimal Dxy { get; set; }
        public decimal Spread { get; set; }

        public PriceTrend NasdaqTrend { get; set; } = new();
        public PriceTrend DollarTrend { get; set; } = new();
    }
}
