using System.Text.Json.Serialization;
using Finsight.Enums;

namespace Finsight.DTOs
{
    public class FSTransactionDTO
    {
        public  Guid Id { get; set; }
      
        public decimal Amount { get; set; }
        
        public decimal BaseAmount { get; set; }

        public Guid CategoryId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public  FSTransactionMode Mode { get; set; }

        public  DateTime Date { get; set; }

        public string Currency { get; set; } = string.Empty;

         [JsonConverter(typeof(JsonStringEnumConverter))]
        public  FSTransactionType Type { get; set; }

        public Guid? SubCategoryId { get; set; }

        public string? Comment { get; set; }

         [JsonConverter(typeof(JsonStringEnumConverter))]
        public FSTransactionSubType? SubType { get; set; }
    }
}