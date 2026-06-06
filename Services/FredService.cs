using MarketRadar.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MarketRadar.Services
{
    public class FredService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey = string.Empty;

        public FredService(HttpClient http, IOptions<Setting> setting)
        {
            _http = http;
            _apiKey = setting.Value.FredApiKey;
        }

        public async Task<decimal> GetYieldDataAsync()
        {
            var ten = await GetSeries("DGS10");
            var two = await GetSeries("DGS2");

            return ten - two;
        }

        private async Task<decimal> GetSeries(string id)
        {
            string url =
                $"https://api.stlouisfed.org/fred/series/observations" +
                $"?series_id={id}&api_key={_apiKey}&file_type=json";

            var json = await _http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);

            var obs = doc.RootElement.GetProperty("observations");

            for (int i = obs.GetArrayLength() - 1; i >= 0; i--)
            {
                var data = obs[i];
                var v = obs[i].GetProperty("value").GetString();

                if (decimal.TryParse(v, out var result))
                    return result;
            }

            return 0;
        }
    }
}
