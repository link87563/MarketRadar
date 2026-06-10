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
            var nasdaqTask = _yahooTrendService.GetTrendAsync("^IXIC");
            var dxyTask = _yahooTrendService.GetTrendAsync("DX-Y.NYB");
            var spreadTask = _fredService.GetYieldSpreadDataAsync();
            var vixTask = _yahooTrendService.GetTrendAsync("^VIX");
            var soxTask = _yahooTrendService.GetTrendAsync("^SOX");
            var tsmTask = _yahooTrendService.GetTrendAsync("TSM");
            var goldTask = _yahooTrendService.GetTrendAsync("GC=F");
            var btcTask = _yahooTrendService.GetTrendAsync("BTC-USD");
            var usdTwdTask = _yahooTrendService.GetTrendAsync("TWD=X");
            var tltTask = _yahooTrendService.GetTrendAsync("TLT");
            var hygTask = _yahooTrendService.GetTrendAsync("HYG");
            var oilTask = _yahooTrendService.GetTrendAsync("CL=F");

            await Task.WhenAll(nasdaqTask, dxyTask, spreadTask, vixTask, soxTask, tsmTask, goldTask, btcTask, usdTwdTask, tltTask, hygTask, oilTask);

            var nasdaq = await nasdaqTask;
            var dxy = await dxyTask;
            var spread = await spreadTask;

            return new MarketData
            {
                NasdaqChange = nasdaq.OneDayChange,
                DollarChange = dxy.OneDayChange,
                NasdaqTrend = nasdaq,
                DollarTrend = dxy,
                YieldSpread = spread.Spread,
                YieldSpreadDate = spread.LatestDate,
                VixTrend = await vixTask,
                SoxTrend = await soxTask,
                TsmTrend = await tsmTask,
                GoldTrend = await goldTask,
                BtcTrend = await btcTask,
                UsdTwdTrend = await usdTwdTask,
                TltTrend = await tltTask,
                HygTrend = await hygTask,
                OilTrend = await oilTask
            };
        }
    }
}
