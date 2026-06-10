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

            var scoreBreakdown = CalculateScoreBreakdown(
                nasdaq,
                dxy,
                spread,
                data.NasdaqTrend,
                data.DollarTrend,
                data.VixTrend,
                data.SoxTrend,
                data.TsmTrend,
                data.TltTrend,
                data.HygTrend);
            var score = scoreBreakdown.Sum(x => x.Score);

            var previousScoreBreakdown = CalculateScoreBreakdown(
                data.NasdaqTrend.PreviousOneDayChange,
                data.DollarTrend.PreviousOneDayChange,
                spread,
                ToPreviousTrend(data.NasdaqTrend),
                ToPreviousTrend(data.DollarTrend),
                ToPreviousTrend(data.VixTrend),
                ToPreviousTrend(data.SoxTrend),
                ToPreviousTrend(data.TsmTrend),
                ToPreviousTrend(data.TltTrend),
                ToPreviousTrend(data.HygTrend));
            var previousScore = previousScoreBreakdown.Sum(x => x.Score);

            return new RiskReport
            {
                Score = score,
                PreviousScore = previousScore,
                ScoreChange = score - previousScore,
                ChangeLabel = GetChangeLabel(score - previousScore),
                Regime = GetRegime(score),
                Stress = GetStress(nasdaq, dxy, data.NasdaqTrend, data.VixTrend, data.SoxTrend, data.HygTrend),
                RiskLabel = GetRiskLabel(score),

                Nasdaq = nasdaq,
                Dxy = dxy,
                Spread = spread,
                YieldSpreadDate = data.YieldSpreadDate,
                NasdaqTrend = data.NasdaqTrend,
                DollarTrend = data.DollarTrend,
                VixTrend = data.VixTrend,
                SoxTrend = data.SoxTrend,
                TsmTrend = data.TsmTrend,
                GoldTrend = data.GoldTrend,
                BtcTrend = data.BtcTrend,
                UsdTwdTrend = data.UsdTwdTrend,
                TltTrend = data.TltTrend,
                HygTrend = data.HygTrend,
                OilTrend = data.OilTrend,
                ScoreBreakdown = scoreBreakdown,
                DataWarnings = GetDataWarnings(data)
            };
        }

        private List<ScoreContribution> CalculateScoreBreakdown(
            decimal nasdaq,
            decimal dxy,
            decimal spread,
            PriceTrend nasdaqTrend,
            PriceTrend dxyTrend,
            PriceTrend vixTrend,
            PriceTrend soxTrend,
            PriceTrend tsmTrend,
            PriceTrend tltTrend,
            PriceTrend hygTrend)
        {
            var nasdaqScore = GetNasdaqScore(nasdaq) * 2;
            var dxyScore = GetDxyScore(dxy);
            var spreadScore = GetSpreadScore(spread);
            var momentumScore = GetMomentumScore(nasdaqTrend, dxyTrend);
            var crossAssetScore = GetCrossAssetScore(vixTrend, soxTrend, tsmTrend);
            var ratesCreditScore = GetRatesCreditScore(tltTrend, hygTrend);

            return new List<ScoreContribution>
            {
                new()
                {
                    Name = "Nasdaq",
                    Score = nasdaqScore,
                    Reason = $"1D {nasdaq:F2}%"
                },
                new()
                {
                    Name = "DXY",
                    Score = dxyScore,
                    Reason = $"1D {dxy:F2}%"
                },
                new()
                {
                    Name = "10Y-2Y",
                    Score = spreadScore,
                    Reason = $"Spread {spread:F2}"
                },
                new()
                {
                    Name = "Momentum",
                    Score = momentumScore,
                    Reason = $"Nasdaq 5D {nasdaqTrend.FiveDayChange:F2}%, DXY 5D {dxyTrend.FiveDayChange:F2}%"
                },
                new()
                {
                    Name = "Cross Asset",
                    Score = crossAssetScore,
                    Reason = $"VIX 5D {FormatTrend(vixTrend)}, SOX 5D {FormatTrend(soxTrend)}, TSM 5D {FormatTrend(tsmTrend)}"
                },
                new()
                {
                    Name = "Rates/Credit",
                    Score = ratesCreditScore,
                    Reason = $"TLT 5D {FormatTrend(tltTrend)}, HYG 5D {FormatTrend(hygTrend)}"
                }
            };
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
            if (score <= -14) return "CRASH";
            if (score <= -8) return "RISK-OFF";
            if (score <= -4) return "CAUTIOUS";
            if (score <= -1) return "CAUTIOUS";
            if (score <= 3) return "NEUTRAL";
            return "RISK-ON";
        }

        private string GetStress(
            decimal nasdaq,
            decimal dxy,
            PriceTrend nasdaqTrend,
            PriceTrend vixTrend,
            PriceTrend soxTrend,
            PriceTrend hygTrend)
        {
            var equityStress =
                nasdaq <= -3 ||
                nasdaqTrend.FiveDayChange <= -5 ||
                soxTrend.FiveDayChange <= -8;

            var volatilityStress = vixTrend.FiveDayChange >= 15;
            var creditStress = hygTrend.IsValid && hygTrend.FiveDayChange <= -2;
            var dollarStress = dxy >= 0.5m;

            if ((equityStress && volatilityStress && (creditStress || dollarStress)) ||
                (nasdaq <= -5 && dollarStress))
            {
                return "HIGH";
            }

            if (nasdaq <= -2 ||
                nasdaqTrend.FiveDayChange <= -3 ||
                soxTrend.FiveDayChange <= -4 ||
                vixTrend.FiveDayChange >= 8 ||
                creditStress)
            {
                return "MEDIUM";
            }

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

        private int GetRatesCreditScore(PriceTrend tlt, PriceTrend hyg)
        {
            var score = 0;

            if (hyg.IsValid)
            {
                if (hyg.FiveDayChange <= -2) score -= 3;
                else if (hyg.FiveDayChange <= -1) score -= 1;
                else if (hyg.FiveDayChange >= 1.5m) score += 1;
            }

            if (tlt.IsValid)
            {
                if (tlt.FiveDayChange <= -3) score -= 2;
                else if (tlt.FiveDayChange >= 3) score += 1;
            }

            return score;
        }

        private int GetCrossAssetScore(PriceTrend vix, PriceTrend sox, PriceTrend tsm)
        {
            var score = 0;

            if (vix.IsValid)
            {
                if (vix.FiveDayChange >= 15) score -= 3;
                else if (vix.FiveDayChange >= 8) score -= 2;
                else if (vix.FiveDayChange <= -10) score += 2;
            }

            if (sox.IsValid)
            {
                if (sox.FiveDayChange <= -4) score -= 2;
                else if (sox.FiveDayChange >= 4) score += 1;
            }

            if (tsm.IsValid)
            {
                if (tsm.FiveDayChange <= -4) score -= 2;
                else if (tsm.FiveDayChange >= 4) score += 1;
            }

            return score;
        }

        private List<string> GetDataWarnings(MarketData data)
        {
            var warnings = new List<string>();

            AddWarning(warnings, "Nasdaq", data.NasdaqTrend);
            AddWarning(warnings, "DXY", data.DollarTrend);
            AddWarning(warnings, "VIX", data.VixTrend);
            AddWarning(warnings, "SOX", data.SoxTrend);
            AddWarning(warnings, "TSM ADR", data.TsmTrend);
            AddWarning(warnings, "Gold", data.GoldTrend);
            AddWarning(warnings, "BTC", data.BtcTrend);
            AddWarning(warnings, "USD/TWD", data.UsdTwdTrend);
            AddWarning(warnings, "TLT", data.TltTrend);
            AddWarning(warnings, "HYG", data.HygTrend);
            AddWarning(warnings, "Oil", data.OilTrend);

            return warnings;
        }

        private void AddWarning(List<string> warnings, string name, PriceTrend trend)
        {
            if (!trend.IsValid)
            {
                warnings.Add($"{name}: {trend.Warning}");
                return;
            }

            if (IsStale(trend.LatestDate))
            {
                warnings.Add($"{name}: 最新資料日期 {trend.LatestDate:yyyy-MM-dd} 可能偏舊");
            }
        }

        private string FormatTrend(PriceTrend trend)
        {
            return trend.IsValid ? $"{trend.FiveDayChange:F2}%" : "N/A";
        }

        private bool IsStale(DateTime? latestDate)
        {
            if (latestDate == null)
                return true;

            return CountBusinessDaysAfter(latestDate.Value.Date, DateTime.Today) > 1;
        }

        private int CountBusinessDaysAfter(DateTime fromExclusive, DateTime toInclusive)
        {
            var count = 0;

            for (var date = fromExclusive.AddDays(1); date <= toInclusive.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday &&
                    date.DayOfWeek != DayOfWeek.Sunday)
                {
                    count++;
                }
            }

            return count;
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
