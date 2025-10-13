using System.ComponentModel.DataAnnotations;
using Finsight.Enums;

namespace Finsight.Commands
{
    public class CreateTransactionCommand()
    {
        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public Guid? CategoryId { get; set; }

        public Guid? SubCategoryId { get; set; }

        [Required(ErrorMessage = "Currency is required.")]
        public string? Currency { get; set; }

        [MaxLength(500, ErrorMessage = "Comment cannot exceed 500 characters.")]
        public string? Comment { get; set; }

        public DateTime? Date { get; set; }

        [Required(ErrorMessage = "Transaction type is required.")]
        public FSTransactionType? Type { get; set; }

        public FSTransactionSubType? SubType { get; set; }

        [Required(ErrorMessage = "Transaction mode is required.")]
        public FSTransactionMode? Mode { get; set; }
    }
}