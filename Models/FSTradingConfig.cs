using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Finsight.Models
{
    [Index(nameof(ServerIp), IsUnique = true)]
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

        public string? FSUserId { get; set; }
        
        public string? ServerIp { get; set; }
        
        public int? ServerPort { get; set; }
        
        public string? AlpacaApiKey { get; set; }
        
        public string? AlpacaApiSecret { get; set; }
        
        public string? Ticker { get; set; }
    }
}
