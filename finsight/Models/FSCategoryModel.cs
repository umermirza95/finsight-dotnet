using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Finsight.Models
{
    public enum FSCategoryTypeEnum
    {
        income,
        expense
    }
    public class FSCategoryModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        [JsonConverter(typeof(StringEnumConverter))]
        public FSCategoryTypeEnum Type { get; set; }
        public List<FSSubCategoryModel>? SubCategories { get; set; }
    }
}