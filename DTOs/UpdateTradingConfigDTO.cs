using System;

namespace Finsight.DTOs
{
    public class UpdateTradingConfigDTO
    {
        public decimal? TradingCapital { get; set; }
        public decimal? TrancheSize { get; set; }
        public bool? AutoTrade { get; set; }
        public decimal? SharesPerTranche { get; set; }
        public decimal? DistancePerTranche { get; set; }
        public bool? LogsOnly { get; set; }
    }
}
