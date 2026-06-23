using System.Collections.Generic;
using Finsight.Models;

namespace Finsight.DTOs
{
    public class OpenTradesResponse
    {
        public List<FSTrade> Trades { get; set; } = new List<FSTrade>();
        public decimal TotalCapital { get; set; }
        public decimal CapitalUsed { get; set; }
        public int AvailableTranches { get; set; }
    }
}
