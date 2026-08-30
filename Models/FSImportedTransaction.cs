using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Finsight.Enums;

namespace Finsight.Models
{
    public class FSImportedTransaction
    {
        [Key]
        [MaxLength(200)]
        public required string Id { get; set; }

        [ForeignKey(nameof(FSUser))]
        public required string FSUserId { get; set; }

        [ForeignKey(nameof(FSWallet))]
        public Guid? FSWalletId { get; set; }

        public required string Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public required decimal Amount { get; set; }

        public required DateOnly Date { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(3)]
        public required string FSCurrencyCode { get; set; }

        public required FSTransactionType Type { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(FSTransaction))]
        public Guid? FSTransactionId { get; set; }
        
        [JsonIgnore]
        public FSTransaction? FSTransaction { get; set; }
    }
}
