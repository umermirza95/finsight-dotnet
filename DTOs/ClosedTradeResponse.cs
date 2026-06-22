using System;

namespace Finsight.DTOs
{
    public class ClosedTradeResponse
    {
        public Guid ClosedTradeId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public DateTime OpenDate { get; set; }
        public DateTime CloseDate { get; set; }
        public decimal Quantity { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public decimal Commission { get; set; }
        public decimal NetProfit { get; set; }
    }
}
