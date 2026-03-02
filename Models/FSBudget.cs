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

        public DateOnly GetEndDate(DateOnly? startDate = null)
        {
            var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            return Frequency switch
            {
                BudgetFrequency.Daily => start,
                BudgetFrequency.Weekly => start.AddDays(6),
                BudgetFrequency.Monthly => start.AddMonths(1).AddDays(-1),
                BudgetFrequency.Yearly => start.AddYears(1).AddDays(-1),
                _ => throw new ArgumentOutOfRangeException(nameof(Frequency), $"Unsupported frequency: {Frequency}")
            };
        }

        public DateOnly GetStartDate(DateOnly? date = null)
        {
            var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

            return Frequency switch
            {
                BudgetFrequency.Daily => targetDate,
                BudgetFrequency.Weekly => targetDate.AddDays(-(int)targetDate.DayOfWeek + (int)DayOfWeek.Monday),
                BudgetFrequency.Monthly => new DateOnly(targetDate.Year, targetDate.Month, 1),
                BudgetFrequency.Yearly => new DateOnly(targetDate.Year, 1, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(Frequency), $"Unsupported frequency: {Frequency}")
            };
        }
    }
}