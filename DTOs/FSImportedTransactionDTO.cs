using System.Text.Json.Serialization;
using Finsight.Enums;

namespace Finsight.DTOs
{
    public class FSImportedTransactionDTO
    {
        public required string Id { get; set; }
        public required string Description { get; set; }
        public decimal Amount { get; set; }
        public DateOnly Date { get; set; }
        public string? BankName { get; set; }
        public required string FSCurrencyCode { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FSTransactionType Type { get; set; }
    }
}
