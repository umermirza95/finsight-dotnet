using System;

namespace Finsight.DTOs
{
    public class ActiveOrderDTO
    {
        public string OrderId { get; set; } = string.Empty;
        public int ConId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // BUY or SELL
        public decimal Quantity { get; set; }
        public decimal LimitPrice { get; set; }
    }
}
