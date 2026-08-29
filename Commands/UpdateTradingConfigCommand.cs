using System;

namespace Finsight.Commands
{
    public class UpdateTradingConfigCommand
    {
        public decimal? TradingCapital { get; set; }
        public decimal? TrancheSize { get; set; }
        public bool? AutoTrade { get; set; }
        public decimal? SharesPerTranche { get; set; }
        public decimal? DistancePerTranche { get; set; }
        public string? ServerIp { get; set; }
        public string? Ticker { get; set; }
    }
}
