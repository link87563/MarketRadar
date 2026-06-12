using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketRadar
{
    public class Setting
    {
        public string FredApiKey { get; set; } = string.Empty;
        public DiscordSetting DiscordSetting { get; set; } = new();
        public GeminiSetting GeminiSetting { get; set; } = new();
        public ShortVideoSetting ShortVideoSetting { get; set; } = new();
    }

    public class DiscordSetting
    {
        public string WebhookUrl { get; set; } = string.Empty;
    }

    public class GeminiSetting
    {
        public bool Enabled { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-2.5-flash-lite";
        public string[] FallbackModels { get; set; } = new[] { "gemini-2.5-flash", "gemini-3.5-flash" };
        public string Prompt { get; set; } = string.Empty;
        public string PromptFile { get; set; } = string.Empty;
        public bool VoiceScriptEnabled { get; set; } = true;
        public string VoiceScriptPrompt { get; set; } = string.Empty;
        public string VoiceScriptPromptFile { get; set; } = string.Empty;
        public int MaxOutputTokens { get; set; } = 1200;
        public int VoiceScriptMaxOutputTokens { get; set; } = 600;
        public int RetryCount { get; set; } = 2;
        public int RetryDelaySeconds { get; set; } = 5;
    }

    public class ShortVideoSetting
    {
        public bool Enabled { get; set; }
        public bool SendToDiscord { get; set; }
        public string OutputDirectory { get; set; } = "outputs";
        public string FfmpegPath { get; set; } = "ffmpeg";
        public string FontName { get; set; } = "Noto Sans CJK TC";
        public int DurationSeconds { get; set; } = 30;
        public int Width { get; set; } = 1080;
        public int Height { get; set; } = 1920;
    }
}
