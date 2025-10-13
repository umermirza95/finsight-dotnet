using System.ComponentModel.DataAnnotations;
using Finsight.Enums;

namespace Finsight.Commands
{
    public class CreateTransactionCommand()
    {
        
        public required decimal Amount { get; set; }
        public required Guid CategoryId { get; set; }
        public Guid? SubCategoryId { get; set; }
        public required string Currency { get; set; }
        public string? Comment { get; set; }
        public DateTime? Date { get; set; }
        public required FSTransactionType Type { get; set; }
        public FSTransactionSubType? SubType { get; set; }
        public required FSTransactionMode Mode { get; set; }
    }
}