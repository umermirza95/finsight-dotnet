using System;

namespace Finsight.DTOs
{
    public class InsurancePayoutDTO
    {
        public Guid Id { get; set; }
        public decimal CoveredAmount { get; set; }
        public Guid? ClosedTradeId { get; set; }
        public DateTime Date { get; set; }
        public string Ticker { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
    }
}
