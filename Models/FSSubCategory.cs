using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finsight.Models
{
    public class FSSubCategory
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [ForeignKey(nameof(FSCategory))]
        public Guid FSCategoryId { get; set; }
    }
}