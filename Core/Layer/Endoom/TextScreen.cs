namespace Helion.Layer.Endoom
{
    using SixLabors.Fonts;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.Drawing.Processing;
    using SixLabors.ImageSharp.PixelFormats;
    using SixLabors.ImageSharp.Processing;
    using System;
    using System.Globalization;
    using System.IO;

    public class TextScreen
    {
        private int m_rows;
        private int m_columns;

        private Color[] m_backgroundColors;
        private Color[] m_foregroundColors;
        private char[] m_characters;
        private bool[] m_blink;
        private Font m_font;
        private int m_pixelHeight;
        private int m_glyphWidth;
        private int m_glyphHeight;

        // This value indicates whether there are any blinking characters in this text screen
        public readonly bool HasBlink;

        /// <summary>
        /// Represents a screen full of double-byte (color plus character) characters, similar to an 80x25 console text buffer
        /// </summary>
        /// <param name="screenData">Raw byte data for the screen</param>
        /// <param name="fontData">Raw bytes for the font data (TrueType font)</param>
        /// <param name="pixelHeight">Pixel height at which text screens will be rendered</param>
        /// <param name="rows">Number of rows in the screen</param>
        /// <param name="columns">Number of columns in the screen</param>
        /// <exception cref="Exception">Thrown if the number of bytes does not match 2 * rows * columns</exception>
        public TextScreen(byte[] screenData, byte[] fontData, int pixelHeight, int rows, int columns)
        {
            if (!(screenData.Length >= rows * columns * 2))
            {
                throw new Exception("Text screen data must contain at least (height * width * 2) bytes");
            }

            m_rows = rows;
            m_columns = columns;
            m_pixelHeight = pixelHeight;

            m_backgroundColors = new Color[rows * columns];
            m_foregroundColors = new Color[rows * columns];
            m_characters = new char[rows * columns];
            m_blink = new bool[rows * columns];

            for (int index = 0; index < rows * columns; index++)
            {
                // See https://en.wikipedia.org/wiki/VGA_text_mode
                // The first byte in each pair is a standard text character (convert to Unicode to use TTF fonts)
                m_characters[index] = Convert.ToChar(Conversions.UnicodeByteMappings[screenData[index * 2]]);

                // The second byte in each pair follows this format:
                // Bytes 0-3: Foreground color
                // Bytes 4-6: Background color
                // Byte 7: Blink enable
                byte colorByte = screenData[index * 2 + 1];
                m_foregroundColors[index] = Conversions.TextColors[(byte)((byte)(colorByte << 4) >> 4)];
                m_backgroundColors[index] = Conversions.TextColors[(byte)((byte)(colorByte << 1) >> 5)];
                HasBlink |= m_blink[index] = (byte)(colorByte >> 7) != 0;
            }

            using (MemoryStream fontDataStream = new MemoryStream(fontData))
            {
                FontCollection fontCollection = new();
                FontFamily consoleFontFamily = fontCollection.Add(fontDataStream, CultureInfo.InvariantCulture);
                m_glyphHeight = pixelHeight / m_rows;
                m_glyphWidth = m_glyphHeight / 2;  // Assume use of 8x16 style fonts
                m_font = consoleFontFamily.CreateFont(m_glyphHeight); // Use whatever pixel value fits all the lines   
            }
        }

        /// <summary>
        /// Generate an ARGB(8,8,8,8) image from this text buffer
        /// </summary>
        /// <param name="blinkOn">If True, then characters marked with "blink" will show background color only in this image</param>
        /// <returns>A rendering of this text buffer</returns>
        public Graphics.Image GenerateImage(bool blinkOn)
        {
            float xOffset = 0, yOffset = 0;
            using (Image<Argb32> bitmap = new Image<Argb32>(m_glyphWidth * m_columns, m_pixelHeight))
            {
                bitmap.Mutate(ctx =>
                {
                    int index = 0;
                    for (int row = 0; row < m_rows; row++)
                    {
                        xOffset = 0;
                        for (int column = 0; column < m_columns; column++)
                        {
                            Color foregroundColor = m_foregroundColors[index];
                            Color backgroundColor = m_backgroundColors[index];
                            char textCharacter = m_characters[index];
                            bool characterBlinking = m_blink[index];

                            ctx.FillPolygon(
                                backgroundColor,
                                new PointF(xOffset, yOffset),
                                new PointF(xOffset + m_glyphWidth, yOffset),
                                new PointF(xOffset + m_glyphWidth, yOffset + m_glyphHeight),
                                new PointF(xOffset, yOffset + m_glyphHeight));

                            if (!(characterBlinking && blinkOn))
                            {
                                ctx.DrawText($"{textCharacter}", m_font, foregroundColor, new PointF() { X = xOffset, Y = yOffset });
                            }
                            xOffset += m_glyphWidth;

                            index++;
                        }
                        yOffset += m_glyphHeight;
                    }
                });

                return Graphics.Image.FromImageSharp(bitmap)!;
            }
        }
    }
}
