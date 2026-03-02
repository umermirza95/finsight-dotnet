
namespace Finsight.DTOs
{
    public class FSBudgetDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public decimal ConsumedAmount { get; set; }
        public string Currency { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }
}