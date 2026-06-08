namespace MarketRadar.Models
{
    public class EconomicCalendarEvent
    {
        public DateTime Date { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Importance { get; set; } = string.Empty;
    }
}
