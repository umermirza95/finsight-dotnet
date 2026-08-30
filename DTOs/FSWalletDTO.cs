using System;

namespace Finsight.DTOs
{
    public class FSWalletDTO
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string FSCurrencyCode { get; set; }
        public DateTime CreationDate { get; set; }
        public decimal InitialBalance { get; set; }
        public decimal Balance { get; set; }
        public int Order { get; set; }
    }
}
