using System.Drawing;

namespace Helion.Graphics.String
{
    /// <summary>
    /// A character that also contains a color.
    /// </summary>
    public struct ColoredChar
    {
        /// <summary>
        /// The character value.
        /// </summary>
        public char Character { get; }

        /// <summary>
        /// The color for the character.
        /// </summary>
        public Color Color { get; }

        public ColoredChar(char c, Color color)
        {
            Character = c;
            Color = color;
        }
    }
}
