using System.ComponentModel.DataAnnotations;
using Finsight.Enums;
using Microsoft.AspNetCore.Components.Forms;

namespace Finsight.Commands
{
    public class CreateTransactionCommand
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public Guid? CategoryId { get; set; }

        public Guid? SubCategoryId { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "USD";

        [StringLength(250)]
        public string? Comment { get; set; }

        private DateOnly? _date { get; set; }

        public DateOnly Date
        {
            get => _date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            set => _date = value;
        }

        [Required]
        public FSTransactionType Type { get; set; }

        public FSTransactionSubType? SubType { get; set; }

        [Required]
        public FSTransactionMode Mode { get; set; }

        public List<IBrowserFile> Attachments { get; set; } = [];
    }
}