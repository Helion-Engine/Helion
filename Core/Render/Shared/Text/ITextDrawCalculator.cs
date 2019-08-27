using System.Drawing;
using Helion.Graphics.String;
using Helion.Util.Geometry;

namespace Helion.Render.Shared.Text
{
    /// <summary>
    /// A helper class used with calculating the area that a string would take
    /// up on the screen.
    /// </summary>
    public interface ITextDrawCalculator
    {
        /// <summary>
        /// Calculates the rectangle draw area of the string for the font and
        /// font size (if any) provided.
        /// </summary>
        /// <param name="str">The string to calculate.</param>
        /// <param name="font">The font to draw with.</param>
        /// <param name="topLeft">The top left draw corner.</param>
        /// <param name="fontSize">The size of the font, or null if it should
        /// use the default size.</param>
        /// <returns>The area the text will be drawn.</returns>
        Rectangle GetDrawArea(ColoredString str, string font, Vec2I topLeft, int? fontSize = null);
    }
}