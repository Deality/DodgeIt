using System.Globalization;

// Arayüzde gösterilen sayılar için ortak biçim: binlikler nokta ile ayrılır (12500 -> "12.500").
public static class NumberFormat
{
    private static readonly NumberFormatInfo DotGroups = new NumberFormatInfo
    {
        NumberGroupSeparator = ".",
        NumberGroupSizes = new[] { 3 },
        NumberDecimalDigits = 0
    };

    public static string Dotted(this int value)
    {
        return value.ToString("N0", DotGroups);
    }
}
