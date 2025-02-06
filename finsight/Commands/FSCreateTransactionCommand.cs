using System.ComponentModel.DataAnnotations;
using Finsight.Models;

namespace Finsight.Command
{
    public class FSCreateTransactionCommand
    {
        [Required]
        [Range(0.01, float.MaxValue, ErrorMessage = "Amount value must be greater than 0.")]
        public float Amount { get; set; }
        [Required]
        public FSTransactionMode Mode { get; set; }
        [Required]
        public string CategoryId { get; set; } = string.Empty;
        public FSSupportedCurrencies? Currency { get; set; }
        public DateTime? Date { get; set; }
        public FSTransactionSubType? SubType { get; set; }
        public bool? UseLiveFx { get; set; }
        public string? Comment { get; set; }
        public bool? AddProcessingFee { get; set; }
        public string? SubCategoryId { get; set; }
    }
}

