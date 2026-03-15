using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Finsight.Enums;

namespace Finsight.Models
{
    public class FSTransactionSuggestion
    {
        [Key]
        public  Guid Id { get; set; }

        [ForeignKey(nameof(FSUser))]
        public  string FSUserId { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public required decimal Amount { get; set; }

        [ForeignKey(nameof(FSCategory))]
        public required Guid FSCategoryId { get; set; }

        public required FSTransactionMode Mode { get; set; }

        public required DateOnly Date { get; set; }

        public  DateTime UpdatedAt { get; set; }

        public required string FSCurrencyCode { get; set; }

        public required FSTransactionType Type { get; set; }
        [ForeignKey(nameof(FSSubCategory))]
        public Guid? FSSubCategoryId { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        public FSTransactionSubType? SubType { get; set; }

        public string TransactionExternalId { get; set; } = string.Empty;
    }
}