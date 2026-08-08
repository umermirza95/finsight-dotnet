using System;
using Finsight.Enums;
using Finsight.Models;

namespace Finsight.DTOs
{
    public class OpenTradeDTO
    {
        public Guid Id { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public decimal TradePrice { get; set; }
        public TradeDirection TradeDirection { get; set; }
        public decimal Quantity { get; set; }
        public decimal Commission { get; set; }
        public DateTime Date { get; set; }
        public string ExternalId { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }

        public static OpenTradeDTO FromEntity(FSTrade trade, decimal currentPrice = 0)
        {
            return new OpenTradeDTO
            {
                Id = trade.Id,
                Ticker = trade.Ticker,
                TradePrice = trade.TradePrice,
                TradeDirection = trade.TradeDirection,
                Quantity = trade.Quantity,
                Commission = trade.Commission,
                Date = trade.Date,
                ExternalId = trade.ExternalId,
                CurrentPrice = currentPrice
            };
        }
    }
}
