namespace MarketRadar.Models
{
    public class EconomicCalendarResult
    {
        public List<EconomicCalendarEvent> Events { get; set; } = new();
        public string Warning { get; set; } = string.Empty;
    }
}
