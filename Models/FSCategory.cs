using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Finsight.Enums;

namespace Finsight.Models
{
    public class FSCategory
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(FSUser))]
        public required string FSUserId { get; set; } 

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public FSTransactionType Type { get; set; }

        // Navigation property
        public ICollection<FSSubCategory> SubCategories { get; set; } = [];

        public ICollection<FSBudgetCategory> BudgetCategories { get; set; } = [];
    }
}