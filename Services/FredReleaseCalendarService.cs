using MarketRadar.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MarketRadar.Services
{
    public class FredReleaseCalendarService
    {
        private static readonly string[] ImportantKeywords =
        {
            "consumer price index",
            "producer price index",
            "gross domestic product",
            "employment situation",
            "unemployment",
            "payroll",
            "retail sales",
            "personal income",
            "personal consumption expenditures",
            "pce",
            "industrial production",
            "ism",
            "manufacturing",
            "fomc",
            "federal open market committee",
            "beige book"
        };

        private static readonly string[] ExcludedReleaseNames =
        {
            "fomc press release",
            "research consumer price index"
        };

        private readonly HttpClient _http;
        private readonly string _apiKey = string.Empty;

        public FredReleaseCalendarService(HttpClient http, IOptions<Setting> setting)
        {
            _http = http;
            _apiKey = setting.Value.FredApiKey;
        }

        public async Task<EconomicCalendarResult> GetUpcomingEventsAsync(int days = 7)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return new EconomicCalendarResult { Warning = "FRED API key is empty; upcoming events skipped." };

            var today = DateTime.Today;
            var end = today.AddDays(days);
            var url =
                "https://api.stlouisfed.org/fred/releases/dates" +
                $"?api_key={_apiKey}" +
                "&file_type=json" +
                "&sort_order=asc" +
                "&limit=1000" +
                $"&realtime_start={today:yyyy-MM-dd}" +
                $"&realtime_end={end:yyyy-MM-dd}" +
                "&include_release_dates_with_no_data=true";

            try
            {
                var json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("release_dates", out var releaseDates))
                    return new EconomicCalendarResult { Warning = "FRED release calendar response did not include release_dates." };

                var events = new List<EconomicCalendarEvent>();

                foreach (var item in releaseDates.EnumerateArray())
                {
                    var name = GetString(item, "release_name");
                    var dateText = GetString(item, "date");

                    if (string.IsNullOrWhiteSpace(dateText))
                        dateText = GetString(item, "release_date");

                    if (!DateTime.TryParse(dateText, out var date))
                        continue;

                    if (date.Date < today || date.Date > end)
                        continue;

                    if (IsExcluded(name) || !IsImportant(name))
                        continue;

                    events.Add(new EconomicCalendarEvent
                    {
                        Date = date.Date,
                        Name = name,
                        Importance = GetImportance(name)
                    });
                }

                return new EconomicCalendarResult
                {
                    Events = events
                        .GroupBy(x => new { x.Date, x.Name })
                        .Select(x => x.First())
                        .OrderBy(x => x.Date)
                        .ThenByDescending(x => x.Importance)
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                return new EconomicCalendarResult { Warning = $"FRED release calendar failed: {ex.Message}" };
            }
        }

        private static bool IsImportant(string name)
        {
            var normalized = name.ToLowerInvariant();
            return ImportantKeywords.Any(normalized.Contains);
        }

        private static bool IsExcluded(string name)
        {
            var normalized = name.ToLowerInvariant();
            return ExcludedReleaseNames.Any(normalized.Contains);
        }

        private static string GetImportance(string name)
        {
            var normalized = name.ToLowerInvariant();

            if (normalized.Contains("consumer price index") ||
                normalized.Contains("employment situation") ||
                normalized.Contains("gross domestic product") ||
                normalized.Contains("fomc") ||
                normalized.Contains("federal open market committee"))
            {
                return "High";
            }

            if (normalized.Contains("producer price index") ||
                normalized.Contains("retail sales") ||
                normalized.Contains("pce") ||
                normalized.Contains("personal consumption expenditures"))
            {
                return "Medium";
            }

            return "Watch";
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value)
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }
    }
}
