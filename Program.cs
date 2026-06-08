using MarketRadar;
using MarketRadar.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

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

        var marketData = await market.GetRiskAsync();

        var report = risk.Calculate(marketData);
        var calendarResult = await calendar.GetUpcomingEventsAsync();
        report.UpcomingEvents = calendarResult.Events;
        report.EconomicCalendarWarning = calendarResult.Warning;

        var msg = formatter.ToConsole(report);

        Log.Information(msg);

        await discord.SendAsync(msg);

    }
}
