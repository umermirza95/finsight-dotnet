using System.ComponentModel.DataAnnotations;
using Finsight.Enums;

namespace Finsight.Commands
{
    public class MakeProfitDistributionCommand
    {
        [Required]
        public decimal Amount { get; set; }

        [Required]
        public ProfitDistributionType Type { get; set; }
    }
}
