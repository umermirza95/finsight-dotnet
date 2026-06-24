using System.Collections.Generic;
using Finsight.Models;

namespace Finsight.DTOs
{
    public class OpenTradesResponse
    {
        public List<OpenTradeDTO> Trades { get; set; } = new List<OpenTradeDTO>();
        public decimal TotalCapital { get; set; }
        public decimal CapitalUsed { get; set; }
        public int AvailableTranches { get; set; }
    }
}
