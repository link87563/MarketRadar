using MarketRadar.Models;
namespace MarketRadar.Services
{
    public class MarketService
    {
        private FredService _fredService;
        private YahooTrendService _yahooTrendService;

        public MarketService(FredService fredService, YahooTrendService yahooTrendService)
        {
            _fredService = fredService;
            _yahooTrendService = yahooTrendService;
        }

        public async Task<MarketData> GetRiskAsync()
        {
            var nasdaqTask = _yahooTrendService.GetTrendWithFallbackAsync("^IXIC", "QQQ");
            var spreadTask = _fredService.GetYieldSpreadDataAsync();

            await Task.WhenAll(nasdaqTask, spreadTask);

            var nasdaq = await nasdaqTask;
            var asOfDate = nasdaq.LatestDate;

            var dxyTask = _yahooTrendService.GetTrendAsync("DX-Y.NYB", asOfDate);
            var vixTask = _yahooTrendService.GetTrendAsync("^VIX", asOfDate);
            var soxTask = _yahooTrendService.GetTrendWithFallbackAsync("^SOX", "SOXX", asOfDate);
            var tsmTask = _yahooTrendService.GetTrendAsync("TSM", asOfDate);
            var goldTask = _yahooTrendService.GetTrendAsync("GC=F", asOfDate);
            var btcTask = _yahooTrendService.GetTrendAsync("BTC-USD", asOfDate);
            var usdTwdTask = _yahooTrendService.GetTrendAsync("TWD=X", asOfDate);
            var tltTask = _yahooTrendService.GetTrendAsync("TLT", asOfDate);
            var hygTask = _yahooTrendService.GetTrendAsync("HYG", asOfDate);
            var oilTask = _yahooTrendService.GetTrendAsync("CL=F", asOfDate);

            await Task.WhenAll(dxyTask, vixTask, soxTask, tsmTask, goldTask, btcTask, usdTwdTask, tltTask, hygTask, oilTask);

            var dxy = await dxyTask;
            var spread = await spreadTask;
            var vix = await vixTask;
            var sox = await soxTask;
            var tsm = await tsmTask;
            var gold = await goldTask;
            var btc = await btcTask;
            var usdTwd = await usdTwdTask;
            var tlt = await tltTask;
            var hyg = await hygTask;
            var oil = await oilTask;

            return new MarketData
            {
                NasdaqChange = nasdaq.OneDayChange,
                DollarChange = dxy.OneDayChange,
                NasdaqTrend = nasdaq,
                DollarTrend = dxy,
                YieldSpread = spread.Spread,
                YieldSpreadDate = spread.LatestDate,
                VixTrend = vix,
                SoxTrend = sox,
                TsmTrend = tsm,
                GoldTrend = gold,
                BtcTrend = btc,
                UsdTwdTrend = usdTwd,
                TltTrend = tlt,
                HygTrend = hyg,
                OilTrend = oil
            };
        }
    }
}
