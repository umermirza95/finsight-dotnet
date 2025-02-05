
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Finsight.Models
{
    public enum FSTransactionType
    {
        income,
        expense
    }

    public enum FSTransactionSubType
    {
        active,
        passive
    }

    public enum FSSupportedCurrencies
    {
        USD,
        PKR,
        AED,
        EUR
    }

    public enum FSTransactionMode
    {
        card,
        cash,
        transfer,
        online
    }

    public class FSTransactionModel
    {
        public string Id { get; set; } = string.Empty;
        public float Amount { get; set; }
        public float BaseAmount { get; set; }
        public string CategoryId { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string? SubCategoryId { get; set; }
        public DateTime Date { get; set; }
        public DateTime UpdatedAt { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FSTransactionType Type {get; set;}

        [JsonConverter(typeof(StringEnumConverter))]
        public FSTransactionSubType? SubType;

        [JsonConverter(typeof(StringEnumConverter))]
        public FSTransactionMode Mode {get; set;}

        [JsonConverter(typeof(StringEnumConverter))]
        public FSSupportedCurrencies Currency { get; set; }

    }
}