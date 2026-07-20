using Finsight.Enums;

namespace Finsight.DTOs
{
    public class BrokerOrderDTO
    {
        public string BrokerOrderId { get; set; } = string.Empty;
        public string Ticker { get; set; } = string.Empty;
        public TradeDirection Direction { get; set; }
        public decimal LimitPrice { get; set; }
        public decimal Quantity { get; set; }
    }
}
