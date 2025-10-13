using System.Text.Json.Serialization;
using Finsight.Enums;

namespace Finsight.DTOs
{
    public class FSCategoryDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FSTransactionType Type { get; set; }
        public ICollection<FSSubCategoryDTO> SubCategories { get; set; } = [];
    }
}