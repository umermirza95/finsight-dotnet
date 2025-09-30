using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Finsight.Enums;


namespace Finsight.Models
{
    public class FSTransaction
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseAmount { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        public Guid? SubCategoryId { get; set; }

        [Required]
        public FSCurrency Currency { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        [Required]
        public FSTransactionType Type { get; set; }

        public FSTransactionSubType? SubType { get; set; }

        [Required]
        public FSTransactionMode Mode { get; set; }
    }
}