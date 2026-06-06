using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketRadar.Models
{
    public class RiskResult
    {
        public int Score { get; set; }
        public string Regime { get; set; }
        public string Stress { get; set; }
    }
}
