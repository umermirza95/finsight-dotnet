using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finsight.Models
{
    public class FSTradingConfig
    {
        [Key]
        public Guid Id { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TradingCapital { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TrancheSize { get; set; }

        public bool AutoTrade { get; set; } = false;

        public bool LogsOnly { get; set; } = true;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SharesPerTranche { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DistancePerTranche { get; set; }

        public string? DefaultUserId { get; set; }
        
        public string? Ticker { get; set; }
    }
}
