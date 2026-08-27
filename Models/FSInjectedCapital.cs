using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finsight.Models
{
    public class FSInjectedCapital
    {
        [Key]
        public required Guid Id { get; set; }

        public required string FSUserId { get; set; }
        [ForeignKey(nameof(FSUserId))]
        public FSUser? User { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public required decimal Amount { get; set; }

        public required DateTime Date { get; set; }
    }
}
