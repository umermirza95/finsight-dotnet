namespace Finsight.Models;

public class Transaction
{
    public string TransactionType { get; set; } = "Income"; // Default value
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD"; // Default value
    public Guid CategoryId { get; set; } = Guid.Empty; // Default value
    public string Comments { get; set; } = string.Empty;
    public string Mode { get; set; } = "Cash"; // Default value

    public DateTime Date { get; set; } = DateTime.Now;
}