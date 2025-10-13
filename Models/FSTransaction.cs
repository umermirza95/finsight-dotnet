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

        [ForeignKey(nameof(FSUser))]
        [Required]
        public string FSUserId { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [ForeignKey(nameof(FSCategory))]
        public Guid FSCategoryId { get; set; }

        [Required]
        public FSTransactionMode Mode { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(FSCurrency))]
        [Required]
        public required string FSCurrencyCode { get; set; }

        [Required]
        public FSTransactionType Type { get; set; }

        [ForeignKey(nameof(FSSubCategory))]
        public Guid? FSSubCategoryId { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        public FSTransactionSubType? SubType { get; set; }


    }
}