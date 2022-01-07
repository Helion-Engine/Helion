using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using MoreLinq;

namespace Helion.Graphics.String;

/// <summary>
/// A string where each character has a color component.
/// </summary>
public class ColoredString
{
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

        StringBuilder builder = new(characters.Count);
        for (int i = 0; i < Characters.Count; i++)
            builder.Append(characters[i].Char);
        String = builder.ToString();
    }
}
