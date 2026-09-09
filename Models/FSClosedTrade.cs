using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finsight.Models
{
    public class FSClosedTrade
    {
        [Key]
        public required Guid Id { get; set; }

        public required string FSUserId { get; set; }
        [ForeignKey(nameof(FSUserId))]
        public FSUser? User { get; set; }

        public required string OrderOpenId { get; set; }
        [ForeignKey(nameof(OrderOpenId))]
        public FSTrade? OpenTrade { get; set; }

        public required string OrderCloseId { get; set; }
        [ForeignKey(nameof(OrderCloseId))]
        public FSTrade? CloseTrade { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClosedQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetProfit { get; set; }

        public FSInsurancePayout? InsurancePayout { get; set; }

        public void CalculateNetProfit(FSTrade openTrade, FSTrade closeTrade, decimal matchedQuantity)
        {
            if (openTrade == null || closeTrade == null)
            {
                throw new ArgumentNullException("Both openTrade and closeTrade must be provided to calculate net profit.");
            }

            ClosedQuantity = matchedQuantity;

            var openComm = openTrade.Quantity > 0 ? (openTrade.Commission * matchedQuantity / openTrade.Quantity) : 0;
            var closeComm = closeTrade.Quantity > 0 ? (closeTrade.Commission * matchedQuantity / closeTrade.Quantity) : 0;

            NetProfit = ((closeTrade.TradePrice - openTrade.TradePrice) * matchedQuantity) - (openComm + closeComm);
        }

        public void RecalculateNetProfit()
        {
            if (OpenTrade != null && CloseTrade != null)
            {
                CalculateNetProfit(OpenTrade, CloseTrade, ClosedQuantity > 0 ? ClosedQuantity : OpenTrade.Quantity);
            }
        }
    }
}
