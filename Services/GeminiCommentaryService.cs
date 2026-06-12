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
            return await GenerateAsync(
                report,
                GetPrompt(_setting.PromptFile, _setting.Prompt, DefaultPrompt),
                _setting.MaxOutputTokens,
                "market commentary");
        }

        public async Task<string> GenerateVoiceScriptAsync(string report, string commentary)
        {
            if (!_setting.VoiceScriptEnabled)
            {
                Log.Information("Gemini voice script is disabled.");
                return string.Empty;
            }

            var input = string.IsNullOrWhiteSpace(commentary)
                ? report
                : $"""
MarketRadar Report:
{report}

Gemini Market Commentary:
{commentary}
""";

            return await GenerateAsync(
                input,
                GetPrompt(_setting.VoiceScriptPromptFile, _setting.VoiceScriptPrompt, DefaultPrompt),
                _setting.VoiceScriptMaxOutputTokens,
                "30s voice script");
        }

        private async Task<string> GenerateAsync(string input, string prompt, int maxOutputTokens, string purpose)
        {
            if (!_setting.Enabled)
            {
                Log.Information("Gemini {Purpose} is disabled.", purpose);
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(_setting.ApiKey))
            {
                Log.Warning("Gemini {Purpose} is enabled but GeminiSetting:ApiKey is empty.", purpose);
                return string.Empty;
            }

            foreach (var model in GetModels())
            {
                for (var attempt = 1; attempt <= Math.Max(1, _setting.RetryCount + 1); attempt++)
                {
                    try
                    {
                        var result = await GenerateWithModelAsync(model, input, prompt, maxOutputTokens, purpose);
                        if (!string.IsNullOrWhiteSpace(result.Text))
                            return result.Text;

                        if (result.ShouldTryNextModel)
                            break;

                        if (!result.ShouldRetry || attempt > _setting.RetryCount)
                            break;

                        var delay = TimeSpan.FromSeconds(Math.Max(1, _setting.RetryDelaySeconds) * attempt);
                        Log.Warning(
                            "Gemini model {Model} is temporarily unavailable for {Purpose}. Retry {Attempt}/{MaxAttempts} after {DelaySeconds}s.",
                            model,
                            purpose,
                            attempt,
                            _setting.RetryCount + 1,
                            delay.TotalSeconds);
                        await Task.Delay(delay);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Gemini {Purpose} generation failed with model {Model}.", purpose, model);
                        break;
                    }
                }
            }

            return string.Empty;
        }

        private async Task<GeminiResult> GenerateWithModelAsync(
            string model,
            string input,
            string prompt,
            int maxOutputTokens,
            string purpose)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{NormalizeModelName(model)}:generateContent";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _setting.ApiKey);
            request.Content = JsonContent.Create(BuildRequest(input, prompt, maxOutputTokens));

            using var response = await _http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning(
                    "Gemini {Purpose} request failed. Model={Model}, StatusCode={StatusCode}, Body={Body}",
                    purpose,
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
                Log.Warning("Gemini {Purpose} response did not contain text. Model={Model}", purpose, model);
                return new GeminiResult(string.Empty, ShouldRetry: false, ShouldTryNextModel: true);
            }

            Log.Information("Gemini {Purpose} generated successfully. Model={Model}", purpose, model);
            return new GeminiResult(text, ShouldRetry: false, ShouldTryNextModel: false);
        }

        private object BuildRequest(string input, string prompt, int maxOutputTokens)
        {
            return new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                        new { text = prompt }
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
                                text = "Please analyze this input:\n\n" + input
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.4,
                    maxOutputTokens = Math.Max(256, maxOutputTokens)
                }
            };
        }

        private string GetPrompt(string promptFile, string prompt, string defaultPrompt)
        {
            if (!string.IsNullOrWhiteSpace(promptFile))
            {
                try
                {
                    var resolvedPromptFile = ResolvePromptFile(promptFile);
                    if (!string.IsNullOrWhiteSpace(resolvedPromptFile))
                        return File.ReadAllText(resolvedPromptFile).Trim();

                    Log.Warning(
                        "Gemini prompt file not found: {PromptFile}. CurrentDirectory={CurrentDirectory}, BaseDirectory={BaseDirectory}",
                        promptFile,
                        Directory.GetCurrentDirectory(),
                        AppContext.BaseDirectory);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to read Gemini prompt file: {PromptFile}", promptFile);
                }
            }

            if (!string.IsNullOrWhiteSpace(prompt))
                return prompt.Trim();

            return defaultPrompt;
        }

        private static string ResolvePromptFile(string promptFile)
        {
            if (Path.IsPathRooted(promptFile))
                return File.Exists(promptFile) ? promptFile : string.Empty;

            var currentDirectoryPath = Path.GetFullPath(promptFile, Directory.GetCurrentDirectory());
            if (File.Exists(currentDirectoryPath))
                return currentDirectoryPath;

            var baseDirectoryPath = Path.GetFullPath(promptFile, AppContext.BaseDirectory);
            if (File.Exists(baseDirectoryPath))
                return baseDirectoryPath;

            return string.Empty;
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
