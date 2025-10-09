using System;
using System.Globalization;

namespace Helion.Util.Parser;

public static class NumberParser
{
    private static readonly NumberFormatInfo DecimalFormat = new() { NumberDecimalSeparator = "." };

    public static bool TryParseDouble(string text, out double d) =>
        double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, DecimalFormat, out d);
    public static bool TryParseDouble(ReadOnlySpan<char> text, out double d) =>
        double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, DecimalFormat, out d);

    public static bool TryParseFloat(string text, out float f) =>
        float.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, DecimalFormat, out f);
    public static bool TryParseFloat(ReadOnlySpan<char> text, out float f) =>
        float.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, DecimalFormat, out f);

}
