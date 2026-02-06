using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Finsight.Models;

namespace Finsight.Models
{
    public class FSBudgetPeriod
    {
        [Key]
        public Guid Id { get; set; }

        public Guid BudgetId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateOnly StartDate { get; set; }

        [ForeignKey(nameof(BudgetId))]
        public virtual FSBudget Budget { get; set; } = null!;
    }
}