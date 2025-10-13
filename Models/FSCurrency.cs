using System.ComponentModel.DataAnnotations;

namespace Finsight.Models
{
    public class FSCurrency
    {
        [Key]
        public string Code { get; set; } = string.Empty;
    }
}