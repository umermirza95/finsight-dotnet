using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finsight.Models
{
    public class FSClosedTrade
    {
        [Key]
        public required Guid Id { get; set; }

        public required string FSUserId { get; set; }
        [ForeignKey(nameof(FSUserId))]
        public FSUser? User { get; set; }

        public required string OrderOpenId { get; set; }
        [ForeignKey(nameof(OrderOpenId))]
        public FSTrade? OpenTrade { get; set; }

        public required string OrderCloseId { get; set; }
        [ForeignKey(nameof(OrderCloseId))]
        public FSTrade? CloseTrade { get; set; }
    }
}
