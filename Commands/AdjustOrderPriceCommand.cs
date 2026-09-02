namespace Finsight.Commands
{
    public class AdjustOrderPriceCommand
    {
        public string OrderId { get; set; } = string.Empty;
        public int ConId { get; set; }
        public decimal NewPrice { get; set; }
        public decimal Quantity { get; set; }
        public string Action { get; set; } = string.Empty;
    }
}
