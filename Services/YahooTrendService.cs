using MarketRadar.Models;
using System.Text.Json;

namespace MarketRadar.Services
{
    public class YahooTrendService
    {
        private readonly HttpClient _http;

        public YahooTrendService(HttpClient http)
        {
            _http = http;
        }

        public async Task<PriceTrend> GetTrendAsync(string symbol)
        {
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}?interval=1d&range=5d";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120 Safari/537.36"
            );

            try
            {
                var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return InvalidTrend(symbol, $"Yahoo request failed: {(int)response.StatusCode}");

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var result = doc.RootElement
                    .GetProperty("chart")
                    .GetProperty("result")[0];

                var timestamps = result.GetProperty("timestamp");
                var closes = result
                    .GetProperty("indicators")
                    .GetProperty("quote")[0]
                    .GetProperty("close");

                var list = new List<decimal>();
                var dates = new List<DateTime>();
                var index = 0;

                foreach (var item in closes.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Number &&
                        item.TryGetDecimal(out var value))
                    {
                        list.Add(value);

                        if (index < timestamps.GetArrayLength() &&
                            timestamps[index].TryGetInt64(out var unixSeconds))
                        {
                            dates.Add(DateTimeOffset.FromUnixTimeSeconds(unixSeconds).LocalDateTime.Date);
                        }
                        else
                        {
                            dates.Add(DateTime.Today);
                        }
                    }

                    index++;
                }

                return BuildTrend(symbol, list, dates);
            }
            catch (Exception ex)
            {
                return InvalidTrend(symbol, $"Yahoo parse failed: {ex.Message}");
            }
        }

        private static PriceTrend BuildTrend(string symbol, List<decimal> closes, List<DateTime> dates)
        {
            if (closes.Count < 2)
                return InvalidTrend(symbol, $"Only {closes.Count} valid close value(s)");

            var latest = closes[^1];
            var oneDayBase = closes[^2];
            var threeDayBase = closes.Count >= 4 ? closes[^4] : closes[0];
            var fiveDayBase = closes[0];

            var recentAverage = closes.TakeLast(Math.Min(3, closes.Count)).Average();
            var previousAverage = closes.Take(Math.Max(1, closes.Count - 3)).Average();

            return new PriceTrend
            {
                OneDayChange = GetChange(latest, oneDayBase),
                ThreeDayChange = GetChange(latest, threeDayBase),
                FiveDayChange = GetChange(latest, fiveDayBase),
                Momentum = GetChange(recentAverage, previousAverage),
                PreviousOneDayChange = GetPreviousOneDayChange(closes),
                PreviousThreeDayChange = GetPreviousThreeDayChange(closes),
                PreviousFiveDayChange = GetPreviousFiveDayChange(closes),
                PreviousMomentum = GetPreviousMomentum(closes),
                DataPoints = closes.Count,
                LatestDate = dates.Count > 0 ? dates[^1] : null,
                IsValid = true
            };
        }

        private static PriceTrend InvalidTrend(string symbol, string warning)
        {
            return new PriceTrend
            {
                IsValid = false,
                Warning = $"{symbol}: {warning}"
            };
        }

        private static decimal GetPreviousOneDayChange(List<decimal> closes)
        {
            if (closes.Count < 3) return 0;
            return GetChange(closes[^2], closes[^3]);
        }

        private static decimal GetPreviousThreeDayChange(List<decimal> closes)
        {
            if (closes.Count < 3) return 0;

            var previousLatest = closes[^2];
            var previousBase = closes.Count >= 5 ? closes[^5] : closes[0];

            return GetChange(previousLatest, previousBase);
        }

        private static decimal GetPreviousFiveDayChange(List<decimal> closes)
        {
            if (closes.Count < 3) return 0;
            return GetChange(closes[^2], closes[0]);
        }

        private static decimal GetPreviousMomentum(List<decimal> closes)
        {
            if (closes.Count < 4) return 0;

            var previousCloses = closes.Take(closes.Count - 1).ToList();
            var recentAverage = previousCloses.TakeLast(Math.Min(3, previousCloses.Count)).Average();
            var previousAverage = previousCloses.Take(Math.Max(1, previousCloses.Count - 3)).Average();

            return GetChange(recentAverage, previousAverage);
        }

        private static decimal GetChange(decimal latest, decimal previous)
        {
            if (previous == 0) return 0;
            return (latest - previous) / previous * 100;
        }
    }
}
