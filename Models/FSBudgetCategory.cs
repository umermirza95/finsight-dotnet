using System.ComponentModel.DataAnnotations.Schema;

namespace Finsight.Models
{
    public class FSBudgetCategory
    {
        public Guid BudgetId { get; set; }

        [ForeignKey(nameof(BudgetId))] // Points to the property below
        public virtual FSBudget Budget { get; set; } = null!;

        public Guid CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))] // Points to the property below
        public virtual FSCategory Category { get; set; } = null!;
    }
}