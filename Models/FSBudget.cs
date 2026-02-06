using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Finsight.Enums;

namespace Finsight.Models
{
    public class FSBudget
    {
        [Key]
        public Guid Id { get; set; }
        public required string FSUserId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [ForeignKey(nameof(FSCurrency))]
        public required string FSCurrencyCode { get; set; }

        public DateOnly StartDate { get; set; }
        
        public BudgetFrequency Frequency { get; set; } = BudgetFrequency.Monthly;

        public ICollection<FSBudgetCategory> BudgetCategories { get; set; } = [];
        public ICollection<FSBudgetPeriod> Periods { get; set; } = [];
    }
}