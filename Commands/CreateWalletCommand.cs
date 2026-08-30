using System.ComponentModel.DataAnnotations;

namespace Finsight.Commands
{
    public class CreateWalletCommand
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "USD";

        public decimal InitialBalance { get; set; } = 0;
    }
}
