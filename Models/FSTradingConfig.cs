using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finsight.Models
{
    public class FSTradingConfig
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string FSUserId { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TradingCapital { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TrancheSize { get; set; }

        public FSUser? User { get; set; }
    }
}
