using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finsight.Models
{
    public class FSInsurancePayout
    {
        [Key]
        public required Guid Id { get; set; }

        public required Guid FSClosedTradeId { get; set; }
        [ForeignKey(nameof(FSClosedTradeId))]
        public FSClosedTrade? ClosedTrade { get; set; }

        public required string FSUserId { get; set; }
        [ForeignKey(nameof(FSUserId))]
        public FSUser? User { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public required decimal CoveredAmount { get; set; }

        public required DateTime CreatedAt { get; set; }
    }
}
