using System;
using Finsight.Models;
using Finsight.Enums;

namespace Finsight.DTOs
{
    public class ProfitDistributionDTO
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public ProfitDistributionType DistributionType { get; set; }
        public string DistributionTypeName => DistributionType.ToString();
        public DateTime Date { get; set; }
    }
}
