namespace Finsight.Commands
{
    public class AdjustOrderPriceCommand
    {
        public int OrderId { get; set; }
        public int ConId { get; set; }
        public decimal NewPrice { get; set; }
        public decimal Quantity { get; set; }
        public string Action { get; set; } = string.Empty;
    }
}
