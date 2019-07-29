using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Helion.Util;

namespace Helion.Graphics.String
{
    /// <summary>
    /// A range for a color in a string.
    /// </summary>
    internal class ColorRange
    {
        /// <summary>
        /// The start index.
        /// </summary>
        public int StartIndex;

        /// <summary>
        /// The end index.
        /// </summary>
        public int EndIndex;

        /// <summary>
        /// The color for the range.
        /// </summary>
        public Color Color;

        public ColorRange(int index, Color color)
        {
            StartIndex = index;
            EndIndex = index;
            Color = color;
        }
    }

    /// <summary>
    /// A helper class for providing coloring methods.
    /// </summary>
    public static class RGBColoredString
    {
        /// <summary>
        /// Takes a list of objects and creates a colored string. Supports any
        /// color objects being converted to a color string.
        /// </summary>
        /// <param name="args">The different argument types.</param>
        /// <returns>The colored string.</returns>
        public static string Create(params object[] args)
        {
            StringBuilder builder = new StringBuilder();

            Color defaultColor = ColoredString.DefaultColor;
            builder.Append($"\\c[{defaultColor.R},{defaultColor.G},{defaultColor.B}]");

            Array.ForEach(args, obj =>
            {
                if (obj is Color color)
                    builder.Append($"\\c[{color.R},{color.G},{color.B}]");
                else
                    builder.Append(obj);
            });

            return builder.ToString();
        }
    }

    /// <summary>
    /// Provides a method that is able to convert a raw string with color codes
    /// into a colored string. A color code is in the form "\C[rrr,ggg,bbb]".
    /// </summary>
    /// 
    /// <example>
    /// The string below:
    /// <code>h\c[123,44,17]i!</code>
    /// is converted to:
    /// <code>
    ///      h [Color = 255, 255, 255]
    ///      i [Color = 123, 44, 17]
    ///      ! [Color = 123, 44, 17]
    /// </code>
    /// Note that the first characters that are not colored are set to some
    /// default color (at the time of writing, this is white).
    /// </example>
    public class RGBColoredStringDecoder
    {
        private static readonly Regex COLOR_REGEX = new Regex(@"(\\c\[\d{1,3},\d{1,3},\d{1,3}\])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static Color ColorDefinitionToColor(string rgbColorCode)
        {
            // TODO: Use ^1 instead of len-1 in C# 8 (so from [3, ^1]).
            int len = rgbColorCode.Length - 4;
            var wtf = rgbColorCode.Substring(3, len);
            string[] colorTriplet = wtf.Split(',');

            if (!int.TryParse(colorTriplet[0], out int r))
                r = ColoredString.DefaultColor.R;
            if (!int.TryParse(colorTriplet[1], out int g))
                g = ColoredString.DefaultColor.G;
            if (!int.TryParse(colorTriplet[2], out int b))
                b = ColoredString.DefaultColor.B;

            return Color.FromArgb(
                (byte)MathHelper.Clamp(r, 0, 255),
                (byte)MathHelper.Clamp(g, 0, 255),
                (byte)MathHelper.Clamp(b, 0, 255)
            );
        }

        /// <summary>
        /// Goes through the string and finds the start/end ranges for each
        /// color code. This will not include the color codes.
        /// </summary>
        /// <param name="str">The string to find the ranges for.</param>
        /// <returns>A list of all the color ranges.</returns>
        private static List<ColorRange> GetColorRanges(string str)
        {
            List<ColorRange> colorRanges = new List<ColorRange>
            {
                new ColorRange(0, ColoredString.DefaultColor),
            };

            MatchCollection matches = COLOR_REGEX.Matches(str);
            foreach (Match match in matches)
            {
                ColorRange currentColorInfo = colorRanges.Last();
                currentColorInfo.EndIndex = match.Index;

                int startIndex = match.Index + match.Length;
                Color color = ColorDefinitionToColor(match.Value);
                colorRanges.Add(new ColorRange(startIndex, color));
            }

            // Since we never set the very last element's ending point due to
            // the loop invariant, we do that now.
            var last = colorRanges.Last();
            last.EndIndex = str.Length;

            // TODO: Convert RemoveAt to use ^1 in C# 8.
            if (last.StartIndex == last.EndIndex)
                colorRanges.RemoveAt(colorRanges.Count - 1);

            return colorRanges;
        }

        /// <summary>
        /// Decodes the string by looking for any "\c[rrr,ggg,bbb] color codes
        /// and turning them into the appropriate colored string.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The colored string for the text provided.</returns>
        public static ColoredString Decode(string str)
        {
            List<ColoredChar> coloredChars = new List<ColoredChar>();

            // TODO: Convert second foreach Substring to hat notation in C# 8.
            foreach (ColorRange range in GetColorRanges(str))
                foreach (char c in str.Substring(range.StartIndex, range.EndIndex - range.StartIndex))
                    coloredChars.Add(new ColoredChar(c, range.Color));

            return new ColoredString(coloredChars);
        }
    }
}
