using System;
using Finsight.Enums;

namespace Finsight.Queries
{
    public class GetTransactionsQuery
    {
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
        public FSTransactionType? Type { get; set; } // "income" or "expense"
        public Guid? CategoryId { get; set; }

        public void ApplyDefaultDateRange()
        {
            if (!From.HasValue || !To.HasValue)
            {
                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                From ??= DateOnly.FromDateTime(startOfMonth);
                To ??= DateOnly.FromDateTime(endOfMonth);
            }
        }
    }
}