namespace Finsight.Commands
{
    public class ManualMatchCommand
    {
        public required string BuyOrderId { get; set; }
        public required string SellOrderId { get; set; }
    }
}
