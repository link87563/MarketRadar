using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace MarketRadar.Services
{
    public class DiscordService
    {
        private readonly HttpClient _http = new();
        private readonly string _webhookUrl = string.Empty;

        public DiscordService(IOptions<Setting> setting)
        {
            _webhookUrl = setting.Value.DiscordSetting.WebhookUrl;
        }

        public async Task SendAsync(string msg)
        {
            if (string.IsNullOrWhiteSpace(_webhookUrl))
                return;

            var body = new
            {
                content = TrimForDiscord(msg)
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.PostAsync(_webhookUrl, content);
            response.EnsureSuccessStatusCode();
        }

        private static string TrimForDiscord(string msg)
        {
            const int maxLength = 2000;

            if (msg.Length <= maxLength)
                return msg;

            return msg[..(maxLength - 20)] + "\n...(truncated)";
        }
    }
}
