namespace Finsight.Enums
{
    public enum FSTransactionType
    {
        income,
        expense,
        transfer_in,
        transfer_out
    }

    public enum FSTransactionSubType
    {
        active,
        passive
    }

    public enum FSTransactionMode
    {
        card,
        cash,
        transfer,
        online
    }

    public enum BudgetFrequency
    {
        Daily,
        Weekly,
        Monthly,
        Yearly
    }

    public enum TradeDirection
    {
        BUY,
        SELL
    }

    public enum ProfitDistributionType
    {
        Insurance,
        Withdrawal,
        Reinvestment
    }

    public enum BrokerType
    {
        IBKR,
        Alpaca
    }
}