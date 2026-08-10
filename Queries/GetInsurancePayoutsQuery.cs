using System;

namespace Finsight.Queries
{
    public class GetInsurancePayoutsQuery
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
