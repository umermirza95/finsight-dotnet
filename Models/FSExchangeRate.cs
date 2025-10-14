
using System.ComponentModel.DataAnnotations;

namespace Finsight.Models
{
    public class FSExchangeRate
    {
        [Key]
        public int Id { get; set; }
        public required string From { get; set; }
        public required string To { get; set; }
        public required double ExchangeRate { get; set; }

        public required DateOnly Date { get; set; }
    }
}