namespace Finsight.Enums
{
    public enum FSTransactionType
    {
        income,
        expense
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
}