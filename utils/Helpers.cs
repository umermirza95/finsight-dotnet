using Finsight.Models;

public static class FSHelpers
{
    public static string GetFXKey(FSExchangeRate fx)
    {
        return $"{fx.From}-{fx.To}-{fx.Date:yyyyMMdd}";
    }

    public static string GetFXKey(string from, string to, DateOnly date)
    {
        return $"{from}-{to}-{date:yyyyMMdd}";
    }
}