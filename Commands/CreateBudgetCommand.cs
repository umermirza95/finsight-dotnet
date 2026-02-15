
using System.ComponentModel.DataAnnotations;
using Finsight.Enums;

namespace Finsight.Commands
{
    public class CreateBudgetCommand
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CurrencyCode { get; set; } = string.Empty;

        [Required]
        public DateOnly StartDate { get; set; }

        [Required]
        public BudgetFrequency Frequency { get; set; }

        [Required]
        public List<Guid> CategoryIds { get; set; } = [];

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
    }

}