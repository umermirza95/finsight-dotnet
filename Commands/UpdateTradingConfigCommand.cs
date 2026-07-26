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
        public bool? LogsOnly { get; set; }
        public string? DefaultUserId { get; set; }
        public string? Ticker { get; set; }
    }
}
