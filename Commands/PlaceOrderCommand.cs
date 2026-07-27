namespace Finsight.Commands
{
    public class PlaceOrderCommand
    {
        public required string Ticker { get; set; }
        public required string Direction { get; set; }
        public decimal Quantity { get; set; }
        public decimal LimitPrice { get; set; }
        public string? Account { get; set; }
    }
}
