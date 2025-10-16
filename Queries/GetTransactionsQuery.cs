using System;
using Finsight.Enums;

namespace Finsight.Queries
{
    public class GetTransactionsQuery
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public FSTransactionType? Type { get; set; } // "income" or "expense"
        public Guid? CategoryId { get; set; }

        public void ApplyDefaultDateRange()
        {
            if (!From.HasValue || !To.HasValue)
            {
                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                From ??= startOfMonth;
                To ??= endOfMonth;
            }
        }
    }
}