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

            foreach (var chunk in SplitForDiscord(msg))
            {
                await SendChunkAsync(chunk);
            }
        }

        private async Task SendChunkAsync(string msg)
        {
            var body = new
            {
                content = msg
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.PostAsync(_webhookUrl, content);
            response.EnsureSuccessStatusCode();
        }

        private static List<string> SplitForDiscord(string msg)
        {
            const int maxLength = 2000;
            var chunks = new List<string>();

            if (msg.Length <= maxLength)
            {
                chunks.Add(msg);
                return chunks;
            }

            var current = new StringBuilder();

            foreach (var line in msg.Replace("\r\n", "\n").Split('\n'))
            {
                var nextLine = current.Length == 0 ? line : Environment.NewLine + line;

                if (current.Length + nextLine.Length > maxLength)
                {
                    if (current.Length > 0)
                    {
                        chunks.Add(current.ToString());
                        current.Clear();
                    }

                    if (line.Length > maxLength)
                    {
                        for (var i = 0; i < line.Length; i += maxLength)
                        {
                            chunks.Add(line.Substring(i, Math.Min(maxLength, line.Length - i)));
                        }

                        continue;
                    }
                }

                if (current.Length > 0)
                    current.AppendLine();

                current.Append(line);
            }

            if (current.Length > 0)
                chunks.Add(current.ToString());

            return chunks;
        }
    }
}
