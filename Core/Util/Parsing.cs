using System.Collections.Generic;
using System.Globalization;

namespace Helion.Util;

public static class Parsing
{
    public static readonly NumberFormatInfo DecimalFormat = new NumberFormatInfo { NumberDecimalSeparator = "." };

    public static double ParseDouble(string value) =>
        double.Parse(CleanForDouble(value), DecimalFormat);

    public static bool TryParseDouble(string value, out double result) =>
        double.TryParse(CleanForDouble(value), DecimalFormat, out result);

    private static string CleanForDouble(string value)
    {
        if (value.Contains(","))
            return value.Replace(",", ".");
        return value;
    }
}
