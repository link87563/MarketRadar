using MarketRadar.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketRadar.Services
{
    public class RiskEngineService
    {
        public RiskReport Calculate(MarketData data)
        {
            var nasdaq = data.NasdaqChange;
            var dxy = data.DollarChange;
            var spread = data.YieldSpread;

            var score = CalculateScore(
                nasdaq,
                dxy,
                spread,
                data.NasdaqTrend,
                data.DollarTrend,
                data.VixTrend,
                data.SoxTrend,
                data.TsmTrend);

            var previousScore = CalculateScore(
                data.NasdaqTrend.PreviousOneDayChange,
                data.DollarTrend.PreviousOneDayChange,
                spread,
                ToPreviousTrend(data.NasdaqTrend),
                ToPreviousTrend(data.DollarTrend),
                ToPreviousTrend(data.VixTrend),
                ToPreviousTrend(data.SoxTrend),
                ToPreviousTrend(data.TsmTrend));

            return new RiskReport
            {
                Score = score,
                PreviousScore = previousScore,
                ScoreChange = score - previousScore,
                ChangeLabel = GetChangeLabel(score - previousScore),
                Regime = GetRegime(score),
                Stress = GetStress(nasdaq, dxy),
                RiskLabel = GetRiskLabel(score),

                Nasdaq = nasdaq,
                Dxy = dxy,
                Spread = spread,
                NasdaqTrend = data.NasdaqTrend,
                DollarTrend = data.DollarTrend,
                VixTrend = data.VixTrend,
                SoxTrend = data.SoxTrend,
                TsmTrend = data.TsmTrend,
                GoldTrend = data.GoldTrend,
                BtcTrend = data.BtcTrend,
                UsdTwdTrend = data.UsdTwdTrend
            };
        }

        private int CalculateScore(
            decimal nasdaq,
            decimal dxy,
            decimal spread,
            PriceTrend nasdaqTrend,
            PriceTrend dxyTrend,
            PriceTrend vixTrend,
            PriceTrend soxTrend,
            PriceTrend tsmTrend)
        {
            return
                GetNasdaqScore(nasdaq) * 2 +
                GetDxyScore(dxy) +
                GetSpreadScore(spread) +
                GetMomentumScore(nasdaqTrend, dxyTrend) +
                GetCrossAssetScore(vixTrend, soxTrend, tsmTrend);
        }

        private PriceTrend ToPreviousTrend(PriceTrend trend)
        {
            return new PriceTrend
            {
                OneDayChange = trend.PreviousOneDayChange,
                ThreeDayChange = trend.PreviousThreeDayChange,
                FiveDayChange = trend.PreviousFiveDayChange,
                Momentum = trend.PreviousMomentum,
                DataPoints = Math.Max(0, trend.DataPoints - 1)
            };
        }

        private int GetNasdaqScore(decimal c)
        {
            if (c <= -5) return -10;
            if (c <= -3) return -7;
            if (c <= -2) return -5;
            if (c <= -1) return -3;
            if (c <= 1) return 0;
            return 3;
        }

        private int GetDxyScore(decimal c)
        {
            if (c >= 1) return -4;
            if (c >= 0.5m) return -2;
            if (c <= -1) return 3;
            if (c <= -0.5m) return 1;
            return 0;
        }

        private int GetSpreadScore(decimal s)
        {
            if (s < 0) return -5;
            if (s < 0.2m) return -2;
            if (s < 0.5m) return -1;
            return 2;
        }

        private string GetRegime(int score)
        {
            if (score <= -8) return "CRASH";
            if (score <= -4) return "RISK-OFF";
            if (score <= -1) return "CAUTIOUS";
            if (score <= 3) return "NEUTRAL";
            return "RISK-ON";
        }

        private string GetStress(decimal nasdaq, decimal dxy)
        {
            if (nasdaq <= -3 && dxy >= 0.5m)
                return "HIGH";

            if (nasdaq <= -2)
                return "MEDIUM";

            return "LOW";
        }

        private int GetMomentumScore(PriceTrend nasdaq, PriceTrend dxy)
        {
            var score = 0;

            if (nasdaq.FiveDayChange <= -5) score -= 4;
            else if (nasdaq.FiveDayChange <= -3) score -= 2;
            else if (nasdaq.FiveDayChange >= 3) score += 2;

            if (dxy.FiveDayChange >= 1) score -= 2;
            else if (dxy.FiveDayChange <= -1) score += 1;

            if (nasdaq.Momentum < -1 && dxy.Momentum > 0.3m) score -= 2;
            if (nasdaq.Momentum > 1 && dxy.Momentum < -0.3m) score += 1;

            return score;
        }

        private int GetCrossAssetScore(PriceTrend vix, PriceTrend sox, PriceTrend tsm)
        {
            var score = 0;

            if (vix.FiveDayChange >= 15) score -= 3;
            else if (vix.FiveDayChange >= 8) score -= 2;
            else if (vix.FiveDayChange <= -10) score += 2;

            if (sox.FiveDayChange <= -4) score -= 2;
            else if (sox.FiveDayChange >= 4) score += 1;

            if (tsm.FiveDayChange <= -4) score -= 2;
            else if (tsm.FiveDayChange >= 4) score += 1;

            return score;
        }

        private string GetRiskLabel(int score)
        {
            if (score <= -12) return "EXTREME RISK";
            if (score <= -8) return "HIGH RISK";
            if (score <= -4) return "ELEVATED RISK";
            if (score <= -1) return "CAUTION";
            if (score <= 2) return "NEUTRAL";
            return "RISK ON";
        }

        private string GetChangeLabel(int scoreChange)
        {
            if (scoreChange <= -5) return "風險明顯惡化";
            if (scoreChange <= -2) return "風險惡化";
            if (scoreChange >= 5) return "風險明顯改善";
            if (scoreChange >= 2) return "風險改善";
            return "風險大致持平";
        }
    }
}
