using System.Drawing;

namespace Helion.Graphics.String;

/// <summary>
/// A character that also contains a color.
/// </summary>
public readonly struct ColoredChar
{
    public readonly char Char;
    public readonly Color Color;

    public ColoredChar(char c, Color color)
    {
        Char = c;
        Color = color;
    }
}
