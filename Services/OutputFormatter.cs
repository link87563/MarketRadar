using MarketRadar.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketRadar.Services
{
    public class OutputFormatter
    {
        public string ToConsole(RiskReport r)
        {
            return $"""
📊 AI Risk Radar 時間：{DateTime.Now:yyyy-MM-dd HH:mm:ss}
━━━━━━━━━━
📌 Market Regime
Regime：{GetRegimeText(r.Regime)}
Risk：{GetRiskText(r.RiskLabel)}
Stress：{GetStressText(r.Stress)}
說明：{GetRegimeDescription(r.Regime)}

📈 Score：{r.Score}
Previous：{r.PreviousScore}
Change：{FormatSigned(r.ScoreChange)}（{r.ChangeLabel}）

━━━━━━━━━━
📡 Market Data
Nasdaq：{r.Nasdaq:F2}%
DXY：{r.Dxy:F2}%
DXY 說明：美元指數，衡量美元相對一籃子主要貨幣的強弱
10Y-2Y：{r.Spread:F2}

━━━━━━━━━━
📈 5D Trend
說明：3D/5D 是近 3/5 個交易日累計變化，Momentum 是最近 3 日均值相對前段均值的變化，用來看短線是否加速。
Nasdaq 3D：{r.NasdaqTrend.ThreeDayChange:F2}%
Nasdaq 5D：{r.NasdaqTrend.FiveDayChange:F2}%
Nasdaq Momentum：{r.NasdaqTrend.Momentum:F2}%
DXY 3D：{r.DollarTrend.ThreeDayChange:F2}%
DXY 5D：{r.DollarTrend.FiveDayChange:F2}%
DXY Momentum：{r.DollarTrend.Momentum:F2}%

━━━━━━━━━━
🌐 Cross Asset
說明：VIX 看恐慌情緒，SOX/TSM 看半導體與台股科技鏈，Gold/BTC 看避險與風險偏好，USD/TWD 看台幣資金壓力。
VIX 5D：{r.VixTrend.FiveDayChange:F2}%
SOX 5D：{r.SoxTrend.FiveDayChange:F2}%
TSM ADR 5D：{r.TsmTrend.FiveDayChange:F2}%
Gold 5D：{r.GoldTrend.FiveDayChange:F2}%
BTC 5D：{r.BtcTrend.FiveDayChange:F2}%
USD/TWD 5D：{r.UsdTwdTrend.FiveDayChange:F2}%

━━━━━━━━━━
📌 Interpretation
{GetComment(r)}

━━━━━━━━━━
🧠 Analysis
{GetAnalysis(r)}
""";
        }

        private string GetAnalysis(RiskReport r)
        {
            var lines = new List<string>
            {
                GetRiskAccelerationComment(r),
                GetNasdaqTrendComment(r),
                GetDxyTrendComment(r),
                GetYieldSpreadComment(r),
                GetVixComment(r),
                GetTaiwanTechComment(r),
                GetGoldBtcComment(r),
                GetUsdTwdComment(r)
            };

            return string.Join(Environment.NewLine, lines);
        }

        private string GetRegimeText(string regime)
        {
            return regime switch
            {
                "CRASH" => "崩盤 / 極端避險（CRASH）",
                "RISK-OFF" => "避險模式（RISK-OFF）",
                "CAUTIOUS" => "謹慎偏弱（CAUTIOUS）",
                "NEUTRAL" => "中性盤整（NEUTRAL）",
                "RISK-ON" => "風險承擔（RISK-ON）",
                _ => regime
            };
        }

        private string GetRiskText(string riskLabel)
        {
            return riskLabel switch
            {
                "EXTREME RISK" => "極端風險（EXTREME RISK）",
                "HIGH RISK" => "高風險（HIGH RISK）",
                "ELEVATED RISK" => "風險升溫（ELEVATED RISK）",
                "CAUTION" => "謹慎觀望（CAUTION）",
                "NEUTRAL" => "中性（NEUTRAL）",
                "RISK ON" => "偏多 / 可承擔風險（RISK ON）",
                _ => riskLabel
            };
        }

        private string GetStressText(string stress)
        {
            return stress switch
            {
                "HIGH" => "高壓力（HIGH）",
                "MEDIUM" => "中等壓力（MEDIUM）",
                "LOW" => "低壓力（LOW）",
                _ => stress
            };
        }

        private string GetRegimeDescription(string regime)
        {
            return regime switch
            {
                "CRASH" => "市場同時出現明顯賣壓與避險訊號，應優先控管曝險。",
                "RISK-OFF" => "資金偏向避險，風險資產承壓，追高需要保守。",
                "CAUTIOUS" => "市場略偏弱，方向還沒完全失控，但需要降低衝動交易。",
                "NEUTRAL" => "多空訊號接近均衡，等待更明確方向。",
                "RISK-ON" => "市場偏向承擔風險，風險資產環境較友善。",
                _ => "目前狀態無對應說明。"
            };
        }

        private string GetRiskAccelerationComment(RiskReport r)
        {
            if (r.ScoreChange <= -5)
                return $"短線風險正在加速釋放，分數比前一交易日下降 {Math.Abs(r.ScoreChange)} 分。";

            if (r.ScoreChange <= -2)
                return $"短線風險正在惡化，分數比前一交易日下降 {Math.Abs(r.ScoreChange)} 分。";

            if (r.ScoreChange >= 5)
                return $"短線風險明顯降溫，分數比前一交易日回升 {r.ScoreChange} 分。";

            if (r.ScoreChange >= 2)
                return $"短線風險正在改善，分數比前一交易日回升 {r.ScoreChange} 分。";

            if (r.Score <= -12 && r.NasdaqTrend.Momentum < -1)
                return "短線風險正在加速釋放。";

            if (r.Score <= -8)
                return "短線風險仍偏高，但需要觀察賣壓是否延續。";

            if (r.Score <= -4)
                return "市場風險開始升溫，短線不宜過度追高。";

            if (r.Score >= 4)
                return "短線風險偏低，市場維持風險承擔狀態。";

            return "短線風險目前偏中性，市場方向尚未明確。";
        }

        private string FormatSigned(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }

        private string GetNasdaqTrendComment(RiskReport r)
        {
            if (r.Nasdaq <= -2 && r.NasdaqTrend.FiveDayChange <= -3)
                return $"Nasdaq 不是單日下跌，而是 3-5 日趨勢轉弱，5 日累計 {r.NasdaqTrend.FiveDayChange:F2}%。";

            if (r.Nasdaq <= -2)
                return "Nasdaq 單日跌幅明顯，但 5 日趨勢尚未全面轉弱。";

            if (r.NasdaqTrend.FiveDayChange >= 3)
                return $"Nasdaq 5 日趨勢偏強，累計上漲 {r.NasdaqTrend.FiveDayChange:F2}%。";

            return "Nasdaq 短線趨勢暫時沒有明顯方向。";
        }

        private string GetDxyTrendComment(RiskReport r)
        {
            if (r.Dxy >= 0.5m && r.DollarTrend.FiveDayChange >= 0.5m)
                return $"DXY（美元指數）同時走強，代表避險資金正在升溫，5 日累計 {r.DollarTrend.FiveDayChange:F2}%。";

            if (r.Dxy >= 0.5m)
                return "DXY（美元指數）單日走強，市場有短線避險跡象。";

            if (r.DollarTrend.FiveDayChange <= -0.5m)
                return $"DXY（美元指數）5 日趨勢轉弱，美元避險壓力正在降溫，5 日累計 {r.DollarTrend.FiveDayChange:F2}%。";

            return "DXY（美元指數）短線變化不大，美元避險訊號尚不明顯。";
        }

        private string GetYieldSpreadComment(RiskReport r)
        {
            if (r.Spread < 0)
                return $"10Y-2Y 為 {r.Spread:F2}，殖利率曲線倒掛，總經環境仍偏壓抑。";

            if (r.Spread < 0.2m)
                return $"10Y-2Y 只有 {r.Spread:F2}，殖利率曲線偏平，景氣訊號仍需保守看待。";

            return $"10Y-2Y 還是正的 {r.Spread:F2}，代表殖利率曲線本身不是最壞狀態，但不足以抵銷股市與美元的 risk-off（避險模式，資金降低風險資產曝險）訊號。";
        }

        private string GetVixComment(RiskReport r)
        {
            if (r.VixTrend.FiveDayChange >= 15)
                return $"VIX（恐慌指數）5 日大幅上升 {r.VixTrend.FiveDayChange:F2}%，代表市場避險需求明顯升溫。";

            if (r.VixTrend.FiveDayChange >= 8)
                return $"VIX（恐慌指數）5 日上升 {r.VixTrend.FiveDayChange:F2}%，市場波動壓力正在增加。";

            if (r.VixTrend.FiveDayChange <= -10)
                return $"VIX（恐慌指數）5 日下降 {Math.Abs(r.VixTrend.FiveDayChange):F2}%，代表恐慌情緒正在降溫。";

            return "VIX（恐慌指數）短線變化不大，波動壓力沒有明顯擴大。";
        }

        private string GetTaiwanTechComment(RiskReport r)
        {
            if (r.SoxTrend.FiveDayChange <= -4 && r.TsmTrend.FiveDayChange <= -4)
                return $"SOX（費半）與 TSM ADR 同步轉弱，台股半導體與電子權值股隔日壓力偏高。";

            if (r.SoxTrend.FiveDayChange <= -4)
                return $"SOX（費半）5 日下跌 {r.SoxTrend.FiveDayChange:F2}%，半導體族群外部壓力升高。";

            if (r.TsmTrend.FiveDayChange <= -4)
                return $"TSM ADR 5 日下跌 {r.TsmTrend.FiveDayChange:F2}%，台股權值股需留意補跌壓力。";

            if (r.SoxTrend.FiveDayChange >= 4 && r.TsmTrend.FiveDayChange >= 4)
                return "SOX（費半）與 TSM ADR 同步偏強，台股科技鏈外部環境較友善。";

            return "SOX（費半）與 TSM ADR 未出現同步極端訊號，台股科技鏈壓力暫不算全面。";
        }

        private string GetGoldBtcComment(RiskReport r)
        {
            if (r.GoldTrend.FiveDayChange > 1 && r.BtcTrend.FiveDayChange < -3)
                return $"Gold 上漲、BTC 下跌，資金偏向防禦，風險偏好正在降溫。";

            if (r.GoldTrend.FiveDayChange < -1 && r.BtcTrend.FiveDayChange < -3)
                return "Gold 與 BTC 同步轉弱，可能反映美元壓制或流動性收縮壓力。";

            if (r.BtcTrend.FiveDayChange > 5)
                return $"BTC 5 日上漲 {r.BtcTrend.FiveDayChange:F2}%，高風險資產情緒仍有支撐。";

            return "Gold/BTC 沒有給出強烈單邊訊號，跨資產情緒以股市與美元訊號為主。";
        }

        private string GetUsdTwdComment(RiskReport r)
        {
            if (r.UsdTwdTrend.FiveDayChange >= 0.8m)
                return $"USD/TWD 5 日上升 {r.UsdTwdTrend.FiveDayChange:F2}%，代表台幣偏貶，台股外資資金壓力需留意。";

            if (r.UsdTwdTrend.FiveDayChange <= -0.8m)
                return $"USD/TWD 5 日下降 {Math.Abs(r.UsdTwdTrend.FiveDayChange):F2}%，代表台幣偏升，外資匯率壓力相對降溫。";

            return "USD/TWD 短線變化不大，台幣匯率暫未形成明顯額外壓力。";
        }

        private string GetComment(RiskReport r)
        {
            if (r.RiskLabel == "EXTREME RISK")
                return "市場進入極端風險釋放，避免追高與槓桿曝險";

            if (r.RiskLabel == "HIGH RISK")
                return "市場偏空，建議降低風險部位";

            if (r.RiskLabel == "ELEVATED RISK")
                return "市場開始轉弱，需注意回檔風險";

            if (r.RiskLabel == "CAUTION")
                return "震盪偏弱，觀望為主";

            if (r.RiskLabel == "NEUTRAL")
                return "市場中性，無明確方向";

            return "市場偏多，可小幅風險承擔";
        }
    }
}
