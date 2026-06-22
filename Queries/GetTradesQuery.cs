using System;

namespace Finsight.Queries
{
    public class GetTradesQuery
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Ticker { get; set; }

        public void ApplyDefaultDateRange()
        {
            if (!StartDate.HasValue || !EndDate.HasValue)
            {
                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1);

                StartDate ??= startOfMonth;
                EndDate ??= endOfMonth;
            }
        }
    }
}
