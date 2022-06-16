using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Helion.Graphics.String;

/// <summary>
/// A string where each character has a color component.
/// </summary>
public class ColoredString
{
    private static readonly StringBuilder StringBuilder = new();

    /// <summary>
    /// The default color that should be applied to all colored strings
    /// when no color information is available.
    /// </summary>
    public static readonly Color DefaultColor = Color.White;

    public readonly string String;
    public readonly List<ColoredChar> Characters;

    public ColoredString(List<ColoredChar> characters)
    {
        Characters = characters;

        for (int i = 0; i < Characters.Count; i++)
            StringBuilder.Append(characters[i].Char);
        String = StringBuilder.ToString();
        StringBuilder.Clear();
    }
}
