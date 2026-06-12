using MarketRadar;
using MarketRadar.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.RegularExpressions;

class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.File("logs/MarketRadar-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Starting AI Radar...");

        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                services.Configure<Setting>(context.Configuration);

                services.AddHttpClient<FredService>();
                services.AddHttpClient<FredReleaseCalendarService>();
                services.AddHttpClient<YahooTrendService>();
                services.AddHttpClient<GeminiCommentaryService>();
                services.AddSingleton<RiskEngineService>();
                services.AddSingleton<MarketService>();
                services.AddSingleton<OutputFormatter>();
                services.AddSingleton<DiscordService>();
            })
            .Build();

        // 3️⃣ 取得 service
        var market = host.Services.GetRequiredService<MarketService>();
        var risk = host.Services.GetRequiredService<RiskEngineService>();

        var formatter = host.Services.GetRequiredService<OutputFormatter>();
        var discord = host.Services.GetRequiredService<DiscordService>();
        var calendar = host.Services.GetRequiredService<FredReleaseCalendarService>();
        var gemini = host.Services.GetRequiredService<GeminiCommentaryService>();
        var setting = host.Services.GetRequiredService<IOptions<Setting>>().Value;

        Log.Information("Start fetching market data...");
        var marketData = await market.GetRiskAsync();

        var report = risk.Calculate(marketData);
        var calendarResult = await calendar.GetUpcomingEventsAsync();
        report.UpcomingEvents = calendarResult.Events;
        report.EconomicCalendarWarning = calendarResult.Warning;

        var rawMsg = formatter.ToConsole(report);
        Log.Information("Start calling Gemini API for market commentary...");
        var commentary = await gemini.GenerateAsync(rawMsg);
        var voiceScript = string.Empty;
        var noTimeVoiceScript = string.Empty;
        if (!string.IsNullOrWhiteSpace(commentary))
        {
            if (setting.GeminiSetting.VoiceScriptEnabled)
            {
                Log.Information("Start calling Gemini API for 30s voice script...");
                voiceScript = await gemini.GenerateVoiceScriptAsync(rawMsg, commentary);
                voiceScript = EnsureVoiceScriptTimingMarkers(voiceScript);
                noTimeVoiceScript = RemoveTimingMarkers(voiceScript);
            }
        }

        var msg = string.IsNullOrWhiteSpace(commentary)
            ? rawMsg
            : $"""
{rawMsg}

━━━━━━━━━━
🧠 Gemini Market Commentary
{commentary}
{GetNoTimeVoiceScriptSection(noTimeVoiceScript, report)}
""";

        Log.Information(msg);

        Log.Information("Send report to Discord...");
        await discord.SendAsync(msg);

    }

    private static string GetVoiceScriptSection(string voiceScript)
    {
        if (string.IsNullOrWhiteSpace(voiceScript))
            return string.Empty;

        return $"""

━━━━━━━━━━
🎬 30s Short Video Script
{voiceScript}
""";
    }

    private static string GetNoTimeVoiceScriptSection(string noTimeVoiceScript, MarketRadar.Models.RiskReport report)
    {
        if (string.IsNullOrWhiteSpace(noTimeVoiceScript))
            return string.Empty;

        var dataDate = report.NasdaqTrend.LatestDate?.ToString("yyyy-MM-dd") ?? "N/A";

        return $"""

━━━━━━━━━━
🎬 Short Video Script（No Time）
日期：{dataDate}

{noTimeVoiceScript}
""";
    }

    private static string RemoveTimingMarkers(string text)
    {
        var result = text;

        result = Regex.Replace(
            result,
            @"[（(]\s*\d+\s*[-~～－]\s*\d+\s*秒\s*[）)]",
            string.Empty);

        result = Regex.Replace(
            result,
            @"[（(]\s*\d+\s*秒\s*[）)]",
            string.Empty);

        return Regex.Replace(result, @"[ \t]+\r?\n", Environment.NewLine).Trim();
    }

    private static string EnsureVoiceScriptTimingMarkers(string voiceScript)
    {
        if (string.IsNullOrWhiteSpace(voiceScript) ||
            Regex.IsMatch(voiceScript, @"[（(]\s*0\s*[-~～－]\s*3\s*秒\s*[）)]"))
        {
            return voiceScript;
        }

        var narration = ExtractSection(voiceScript, "口播稿", "畫面重點");
        if (string.IsNullOrWhiteSpace(narration))
            return voiceScript;

        var sentences = Regex
            .Split(narration.Replace("\r\n", "\n"), @"(?<=[。！？!?\n])")
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (sentences.Count == 0)
            return voiceScript;

        var segments = new[]
        {
            TakeSentences(sentences, 0, 1),
            TakeSentences(sentences, 1, 1),
            TakeSentences(sentences, 2, 1),
            TakeSentences(sentences, 3, 1),
            TakeSentences(sentences, 4, Math.Max(1, sentences.Count - 4))
        };

        var rebuiltNarration = $"""
（0-3秒）{segments[0]}
（3-10秒）{segments[1]}
（10-18秒）{segments[2]}
（18-25秒）{segments[3]}
（25-30秒）{segments[4]}
""";

        return ReplaceSection(voiceScript, "口播稿", "畫面重點", rebuiltNarration.Trim());
    }

    private static string ExtractSection(string text, string startLabel, string endLabel)
    {
        var startMatch = Regex.Match(text, $"{Regex.Escape(startLabel)}\\s*[：:]");
        if (!startMatch.Success)
            return string.Empty;

        var startIndex = startMatch.Index + startMatch.Length;
        var endMatch = Regex.Match(text[startIndex..], $"{Regex.Escape(endLabel)}\\s*[：:]");
        var endIndex = endMatch.Success ? startIndex + endMatch.Index : text.Length;

        return text[startIndex..endIndex].Trim();
    }

    private static string ReplaceSection(string text, string startLabel, string endLabel, string replacement)
    {
        var startMatch = Regex.Match(text, $"{Regex.Escape(startLabel)}\\s*[：:]");
        if (!startMatch.Success)
            return text;

        var startIndex = startMatch.Index + startMatch.Length;
        var endMatch = Regex.Match(text[startIndex..], $"{Regex.Escape(endLabel)}\\s*[：:]");
        if (!endMatch.Success)
            return text;

        var endIndex = startIndex + endMatch.Index;
        return text[..startIndex] + Environment.NewLine + replacement + Environment.NewLine + Environment.NewLine + text[endIndex..];
    }

    private static string TakeSentences(List<string> sentences, int start, int count)
    {
        if (start >= sentences.Count)
            return sentences.Last();

        return string.Join(string.Empty, sentences.Skip(start).Take(count));
    }
}
