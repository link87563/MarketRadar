namespace MarketRadar.Models
{
    public class PriceTrend
    {
        public decimal OneDayChange { get; set; }
        public decimal ThreeDayChange { get; set; }
        public decimal FiveDayChange { get; set; }
        public decimal Momentum { get; set; }
        public decimal PreviousOneDayChange { get; set; }
        public decimal PreviousThreeDayChange { get; set; }
        public decimal PreviousFiveDayChange { get; set; }
        public decimal PreviousMomentum { get; set; }
        public int DataPoints { get; set; }
        public DateTime? LatestDate { get; set; }
        public bool IsValid { get; set; }
        public string Warning { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string FallbackFromSymbol { get; set; } = string.Empty;
    }
}
