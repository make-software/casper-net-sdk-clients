namespace CasperERC20.Utils;

public static class FormatExtensions
{
    public static string FormatHash(this string hash)
    {
        var len = hash.Length;
        return $"{hash.Substring(0, 8)}....{hash.Substring(len-8, 8)}";
    }
}