using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finsight.Models
{
    public class FSWallet
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(FSUser))]
        public required string FSUserId { get; set; }

        [Required]
        public required string Name { get; set; }

        [Required]
        [ForeignKey(nameof(FSCurrency))]
        public required string FSCurrencyCode { get; set; }

        public DateTime CreationDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal InitialBalance { get; set; }

        public int Order { get; set; } = 0;
    }
}
