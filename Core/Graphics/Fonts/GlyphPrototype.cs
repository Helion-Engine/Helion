using Helion.Resources.Definitions.Fonts.Definition;

namespace Helion.Graphics.Fonts
{
    /// <summary>
    /// Used by the <see cref="FontCompiler"/> to hold a glyph temporarily.
    /// </summary>
    internal class GlyphPrototype
    {
        internal readonly Image Image;
        internal readonly FontAlignment? Alignment;

        internal GlyphPrototype(Image image, FontAlignment? alignment)
        {
            Image = image;
            Alignment = alignment;
        }
    }
}