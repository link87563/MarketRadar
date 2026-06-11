using Microsoft.Extensions.Options;
using Serilog;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MarketRadar.Services
{
    public class GeminiCommentaryService
    {
        private const string DefaultPrompt = """
You are a senior macro analyst. Analyze only the provided MarketRadar report.
Do not invent data, news, events, or prices. If the report is insufficient, say so clearly.
Write a concise Traditional Chinese market commentary with: summary, key drivers, 3-5 day watchlist, credit market assessment, Taiwan tech impact, and conclusion.
""";

        private readonly HttpClient _http;
        private readonly GeminiSetting _setting;

        public GeminiCommentaryService(HttpClient http, IOptions<Setting> setting)
        {
            _http = http;
            _setting = setting.Value.GeminiSetting;
        }

        public async Task<string> GenerateAsync(string report)
        {
            if (!_setting.Enabled)
            {
                Log.Information("Gemini commentary is disabled.");
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(_setting.ApiKey))
            {
                Log.Warning("Gemini commentary is enabled but GeminiSetting:ApiKey is empty.");
                return string.Empty;
            }

            foreach (var model in GetModels())
            {
                for (var attempt = 1; attempt <= Math.Max(1, _setting.RetryCount + 1); attempt++)
                {
                    try
                    {
                        var result = await GenerateWithModelAsync(model, report);
                        if (!string.IsNullOrWhiteSpace(result.Text))
                            return result.Text;

                        if (result.ShouldTryNextModel)
                            break;

                        if (!result.ShouldRetry || attempt > _setting.RetryCount)
                            break;

                        var delay = TimeSpan.FromSeconds(Math.Max(1, _setting.RetryDelaySeconds) * attempt);
                        Log.Warning(
                            "Gemini model {Model} is temporarily unavailable. Retry {Attempt}/{MaxAttempts} after {DelaySeconds}s.",
                            model,
                            attempt,
                            _setting.RetryCount + 1,
                            delay.TotalSeconds);
                        await Task.Delay(delay);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Gemini commentary generation failed with model {Model}.", model);
                        break;
                    }
                }
            }

            return string.Empty;
        }

        private async Task<GeminiResult> GenerateWithModelAsync(string model, string report)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{NormalizeModelName(model)}:generateContent";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _setting.ApiKey);
            request.Content = JsonContent.Create(BuildRequest(report));

            using var response = await _http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning(
                    "Gemini commentary request failed. Model={Model}, StatusCode={StatusCode}, Body={Body}",
                    model,
                    response.StatusCode,
                    body);

                return new GeminiResult(
                    Text: string.Empty,
                    ShouldRetry: IsRetryable(response.StatusCode),
                    ShouldTryNextModel: response.StatusCode == HttpStatusCode.NotFound ||
                        response.StatusCode == HttpStatusCode.BadRequest);
            }

            var text = ExtractText(body);
            if (string.IsNullOrWhiteSpace(text))
            {
                Log.Warning("Gemini commentary response did not contain text. Model={Model}", model);
                return new GeminiResult(string.Empty, ShouldRetry: false, ShouldTryNextModel: true);
            }

            Log.Information("Gemini commentary generated successfully. Model={Model}", model);
            return new GeminiResult(text, ShouldRetry: false, ShouldTryNextModel: false);
        }

        private object BuildRequest(string report)
        {
            return new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                        new { text = GetPrompt() }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = "Please analyze this MarketRadar raw report:\n\n" + report
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.4,
                    maxOutputTokens = Math.Max(256, _setting.MaxOutputTokens)
                }
            };
        }

        private string GetPrompt()
        {
            if (!string.IsNullOrWhiteSpace(_setting.PromptFile))
            {
                try
                {
                    if (File.Exists(_setting.PromptFile))
                        return File.ReadAllText(_setting.PromptFile).Trim();

                    Log.Warning("Gemini prompt file not found: {PromptFile}", _setting.PromptFile);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to read Gemini prompt file: {PromptFile}", _setting.PromptFile);
                }
            }

            if (!string.IsNullOrWhiteSpace(_setting.Prompt))
                return _setting.Prompt.Trim();

            return DefaultPrompt;
        }

        private IEnumerable<string> GetModels()
        {
            return new[] { _setting.Model }
                .Concat(_setting.FallbackModels ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizeModelName(string model)
        {
            const string prefix = "models/";
            return model.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? model[prefix.Length..]
                : model;
        }

        private static bool IsRetryable(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.TooManyRequests ||
                statusCode == HttpStatusCode.ServiceUnavailable ||
                statusCode == HttpStatusCode.GatewayTimeout ||
                statusCode == HttpStatusCode.BadGateway;
        }

        private static string ExtractText(string json)
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            var firstCandidate = candidates[0];
            if (!firstCandidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts))
            {
                return string.Empty;
            }

            var lines = new List<string>();
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        lines.Add(text.Trim());
                }
            }

            return string.Join(Environment.NewLine, lines).Trim();
        }

        private sealed record GeminiResult(string Text, bool ShouldRetry, bool ShouldTryNextModel);
    }
}
