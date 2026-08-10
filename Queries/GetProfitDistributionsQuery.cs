using System;
using Finsight.Models;
using Finsight.Enums;

namespace Finsight.Queries
{
    public class GetProfitDistributionsQuery
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ProfitDistributionType? DistributionType { get; set; }
    }
}
