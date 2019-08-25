namespace Helion.Graphics.Fonts
{
    /// <summary>
    /// An atomic element of a font, which is a character with an image.
    /// </summary>
    public class Glyph
    {
        /// <summary>
        /// The character that makes up this glyph.
        /// </summary>
        public char Character { get; }

        /// <summary>
        /// The image for this glyph.
        /// </summary>
        public Image Image { get; }

        public Glyph(char c, Image image)
        {
            Character = c;
            Image = image;
        }
    }
}
