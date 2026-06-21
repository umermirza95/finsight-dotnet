using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Finsight.Enums;

namespace Finsight.Models
{
    public class FSTrade
    {
        [Key]
        public required Guid Id { get; set; }

        [ForeignKey(nameof(FSUser))]
        public required string FSUserId { get; set; }

        public required string Ticker { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public required decimal TradePrice { get; set; }

        public required TradeDirection TradeDirection { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public required decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public required decimal Commission { get; set; }

        public required DateOnly Date { get; set; }

        public required string ExternalId { get; set; }
    }
}
