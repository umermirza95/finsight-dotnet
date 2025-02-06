using System.ComponentModel.DataAnnotations;
using Finsight.Models;

namespace Finsight.Command
{
    public class FSCreateTransactionCommand
    {
        [Required]
        public float Amount { get; set; }
        public bool? AddProcessingFee { get; set; }
        [Required]
        public string CategoryId { get; set; } = string.Empty;
        public string? SubCategoryId { get; set; }
        public FSSupportedCurrencies? Currency { get; set; }
        public string? Comment { get; set; }
        public DateTime? Date { get; set; }
        public FSTransactionSubType? SubType { get; set; }
        [Required]
        public FSTransactionMode Mode { get; set; }
        public bool? UseLiveFx { get; set; }
    }
}

