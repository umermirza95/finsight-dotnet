using Finsight.Models;

public static class FSHelpers
{
    public static string GetFXKey(string from, string to, DateOnly date)
    {
        return $"{from}-{to}-{date:yyyyMMdd}";
    }
}