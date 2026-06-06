using MarketRadar.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketRadar.Services
{
    public class MarketService
    {
        private FredService _fredService;
        private NasdaqService _nasdaqService;
        private DxyService _dxyService;

        public MarketService(IOptions<Setting> setting, FredService fredService, NasdaqService nasdaqService, 
            DxyService dxyService)
        {
            _fredService = fredService;
            _nasdaqService = nasdaqService;
            _dxyService = dxyService;
        }

        public async Task<MarketData> GetRiskAsync()
        {
            var nasdaq = await _nasdaqService.GetNasdaqDataAsync();
            var dxy = await _dxyService.GetDxyDataAsync();
            var spread = await _fredService.GetYieldDataAsync();

            return new MarketData
            {
                NasdaqChange = nasdaq.OneDayChange,
                DollarChange = dxy.OneDayChange,
                NasdaqTrend = nasdaq,
                DollarTrend = dxy,
                YieldSpread = spread
            };
        }
    }
}
