using MarketRadar.Models;
using Microsoft.Extensions.Options;
using Serilog;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace MarketRadar.Services
{
    public class ShortVideoService
    {
        private const string TitleLabel = "\u6a19\u984c";
        private const string NarrationLabel = "\u53e3\u64ad\u7a3f";
        private const string VisualPointsLabel = "\u756b\u9762\u91cd\u9ede";

        private readonly ShortVideoSetting _setting;

        public ShortVideoService(IOptions<Setting> setting)
        {
            _setting = setting.Value.ShortVideoSetting;
        }

        public async Task<string> CreateAsync(RiskReport report, string voiceScript)
        {
            if (!_setting.Enabled || string.IsNullOrWhiteSpace(voiceScript))
                return string.Empty;

            try
            {
                var outputDirectory = Path.GetFullPath(
                    string.IsNullOrWhiteSpace(_setting.OutputDirectory)
                        ? "outputs"
                        : _setting.OutputDirectory);

                Directory.CreateDirectory(outputDirectory);

                var dataDate = report.NasdaqTrend.LatestDate?.ToString("yyyyMMdd") ?? DateTime.Today.ToString("yyyyMMdd");
                var baseName = $"short-video-{dataDate}";
                var assPath = Path.Combine(outputDirectory, $"{baseName}.ass");
                var videoPath = Path.Combine(outputDirectory, $"{baseName}.mp4");

                var script = ShortVideoScript.Parse(voiceScript);
                await File.WriteAllTextAsync(assPath, BuildAss(report, script), new UTF8Encoding(false));

                var success = await RunFfmpegAsync(outputDirectory, Path.GetFileName(assPath), Path.GetFileName(videoPath));
                if (!success || !File.Exists(videoPath))
                    return string.Empty;

                Log.Information("Short video created: {Path}", videoPath);
                return videoPath;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to create short video.");
                return string.Empty;
            }
        }

        private async Task<bool> RunFfmpegAsync(string workingDirectory, string assFileName, string videoFileName)
        {
            var duration = Math.Clamp(_setting.DurationSeconds, 10, 60);
            var width = Math.Clamp(_setting.Width, 720, 2160);
            var height = Math.Clamp(_setting.Height, 1280, 3840);
            var ffmpegPath = ResolveExecutablePath(string.IsNullOrWhiteSpace(_setting.FfmpegPath) ? "ffmpeg" : _setting.FfmpegPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                WorkingDirectory = workingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-y");
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add("lavfi");
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add($"color=c=0x0B1020:s={width}x{height}:r=30:d={duration}");
            startInfo.ArgumentList.Add("-vf");
            startInfo.ArgumentList.Add($"subtitles={assFileName}");
            startInfo.ArgumentList.Add("-c:v");
            startInfo.ArgumentList.Add("libx264");
            startInfo.ArgumentList.Add("-pix_fmt");
            startInfo.ArgumentList.Add("yuv420p");
            startInfo.ArgumentList.Add("-r");
            startInfo.ArgumentList.Add("30");
            startInfo.ArgumentList.Add("-crf");
            startInfo.ArgumentList.Add("28");
            startInfo.ArgumentList.Add("-movflags");
            startInfo.ArgumentList.Add("+faststart");
            startInfo.ArgumentList.Add(videoFileName);

            Process? process;
            try
            {
                process = Process.Start(startInfo);
            }
            catch (Win32Exception ex)
            {
                Log.Warning(
                    ex,
                    "FFmpeg executable not found. Install FFmpeg, restart the terminal/IDE, or set ShortVideoSetting:FfmpegPath to the full ffmpeg.exe path. Current value: {FfmpegPath}, Resolved value: {ResolvedFfmpegPath}",
                    _setting.FfmpegPath,
                    ffmpegPath);
                return false;
            }

            if (process == null)
            {
                Log.Warning("Failed to start FFmpeg.");
                return false;
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode == 0)
                return true;

            Log.Warning("FFmpeg failed. ExitCode={ExitCode}, Stdout={Stdout}, Stderr={Stderr}", process.ExitCode, stdout, stderr);
            return false;
        }

        private string BuildAss(RiskReport report, ShortVideoScript script)
        {
            var title = string.IsNullOrWhiteSpace(script.Title) ? "AI Risk Radar" : script.Title;
            var narration = string.IsNullOrWhiteSpace(script.Narration)
                ? "\u5e02\u5834\u77ed\u7dda\u8a0a\u865f\u4ecd\u9700\u89c0\u5bdf\uff0c\u8acb\u4ee5\u8cc7\u6599\u54c1\u8cea\u8207\u6838\u5fc3\u6307\u6a19\u70ba\u6e96\u3002"
                : script.Narration;
            var visualPoints = string.IsNullOrWhiteSpace(script.VisualPoints)
                ? "\u95dc\u6ce8 Score\u3001VIX\u3001SOX\u3001HYG \u8207\u7f8e\u5143\u6307\u6578\u3002"
                : script.VisualPoints;

            var firstHalf = TakeText(narration, 70);
            var secondHalf = TakeTailText(narration, 80);
            var metrics = $"Score {report.Score} ({FormatSigned(report.ScoreChange)})\\NRegime {report.Regime} | Risk {report.RiskLabel}\\NNasdaq 5D {report.NasdaqTrend.FiveDayChange:F2}% | VIX 5D {report.VixTrend.FiveDayChange:F2}%\\NSOX 5D {report.SoxTrend.FiveDayChange:F2}% | HYG 5D {report.HygTrend.FiveDayChange:F2}%";

            var events = new[]
            {
                BuildDialogue(0, 5, title, metrics),
                BuildDialogue(5, 12, "\u5e02\u5834\u72c0\u614b", firstHalf),
                BuildDialogue(12, 19, "\u98a8\u96aa\u4e3b\u56e0", metrics),
                BuildDialogue(19, 25, "\u756b\u9762\u91cd\u9ede", visualPoints),
                BuildDialogue(25, 30, "\u7d50\u8ad6", secondHalf)
            };

            return $$"""
[Script Info]
ScriptType: v4.00+
PlayResX: 1080
PlayResY: 1920
ScaledBorderAndShadow: yes

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Title,{{_setting.FontName}},74,&H00FFFFFF,&H000000FF,&H0011182A,&HAA000000,-1,0,0,0,100,100,0,0,1,3,1,5,80,80,0,1
Style: Body,{{_setting.FontName}},46,&H00F4F7FB,&H000000FF,&H0011182A,&HAA000000,0,0,0,0,100,100,0,0,1,2,1,5,90,90,0,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
{{string.Join(Environment.NewLine, events)}}
""";
        }

        private string BuildDialogue(int startSecond, int endSecond, string title, string body)
        {
            var text = "{\\fad(250,250)\\pos(540,300)\\an5}" +
                EscapeAss(WrapText(title, 12)) +
                "{\\fs42}\\N\\N" +
                EscapeAss(WrapText(body, 20));

            return $"Dialogue: 0,{FormatAssTime(startSecond)},{FormatAssTime(endSecond)},Title,,0,0,0,,{text}";
        }

        private static string ResolveExecutablePath(string executable)
        {
            if (Path.IsPathRooted(executable))
                return executable;

            if (File.Exists(executable))
                return Path.GetFullPath(executable);

            var extensions = GetExecutableExtensions(executable);
            var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

            foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var extension in extensions)
                {
                    var candidate = Path.Combine(directory.Trim(), executable + extension);
                    if (File.Exists(candidate))
                        return candidate;
                }
            }

            return executable;
        }

        private static IEnumerable<string> GetExecutableExtensions(string executable)
        {
            if (!string.IsNullOrWhiteSpace(Path.GetExtension(executable)))
                return new[] { string.Empty };

            var pathExt = Environment.GetEnvironmentVariable("PATHEXT");
            if (string.IsNullOrWhiteSpace(pathExt))
                return new[] { ".exe", ".cmd", ".bat", string.Empty };

            return pathExt
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Concat(new[] { string.Empty })
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string FormatAssTime(int seconds)
        {
            return $"0:00:{seconds:00}.00";
        }

        private static string WrapText(string text, int maxCharsPerLine)
        {
            var normalized = Regex.Replace(text.Replace("\r\n", "\n").Replace('\r', '\n'), @"\s+", " ").Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;

            var lines = new List<string>();
            var current = new StringBuilder();

            foreach (var ch in normalized)
            {
                current.Append(ch);
                if (current.Length >= maxCharsPerLine)
                {
                    lines.Add(current.ToString());
                    current.Clear();
                }
            }

            if (current.Length > 0)
                lines.Add(current.ToString());

            return string.Join("\\N", lines.Take(8));
        }

        private static string EscapeAss(string text)
        {
            return text
                .Replace("{", "[")
                .Replace("}", "]")
                .Replace("\n", "\\N");
        }

        private static string TakeText(string text, int maxChars)
        {
            var clean = Regex.Replace(text, @"\s+", " ").Trim();
            return clean.Length <= maxChars ? clean : clean[..maxChars] + "...";
        }

        private static string TakeTailText(string text, int maxChars)
        {
            var clean = Regex.Replace(text, @"\s+", " ").Trim();
            if (clean.Length <= maxChars)
                return clean;

            return clean[^maxChars..];
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }

        private sealed record ShortVideoScript(string Title, string Narration, string VisualPoints)
        {
            public static ShortVideoScript Parse(string text)
            {
                return new ShortVideoScript(
                    ExtractSection(text, TitleLabel, NarrationLabel),
                    ExtractSection(text, NarrationLabel, VisualPointsLabel),
                    ExtractSection(text, VisualPointsLabel, null));
            }

            private static string ExtractSection(string text, string startLabel, string? endLabel)
            {
                var startPattern = $"{Regex.Escape(startLabel)}\\s*[\uff1a:]";
                var startMatch = Regex.Match(text, startPattern);
                if (!startMatch.Success)
                    return string.Empty;

                var startIndex = startMatch.Index + startMatch.Length;
                var endIndex = text.Length;

                if (!string.IsNullOrWhiteSpace(endLabel))
                {
                    var endMatch = Regex.Match(text[startIndex..], $"{Regex.Escape(endLabel)}\\s*[\uff1a:]");
                    if (endMatch.Success)
                        endIndex = startIndex + endMatch.Index;
                }

                return text[startIndex..endIndex].Trim();
            }
        }
    }
}
