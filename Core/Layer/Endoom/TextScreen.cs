namespace Helion.Layer.Endoom
{
    using SixLabors.Fonts;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.Advanced;
    using SixLabors.ImageSharp.Drawing.Processing;
    using SixLabors.ImageSharp.PixelFormats;
    using SixLabors.ImageSharp.Processing;
    using System;
    using System.IO;

    public class TextScreen
    {
        private int m_height;
        private int m_width;

        private Color[] m_backgroundColors;
        private Color[] m_foregroundColors;
        private char[] m_characters;
        private bool[] m_blink;

        // This value indicates whether there are any blinking characters in this text screen
        public readonly bool HasBlink;

        public TextScreen(byte[] screenData, int height, int width)
        {
            if (!(screenData.Length >= height * width * 2))
            {
                throw new Exception("Text screen data must contain at least (height * width * 2) bytes");
            }

            m_height = height;
            m_width = width;

            m_backgroundColors = new Color[height * width];
            m_foregroundColors = new Color[height * width];
            m_characters = new char[height * width];
            m_blink = new bool[height * width];


            for (int index = 0; index < height * width; index++)
            {
                byte textByte = screenData[index * 2];
                byte colorByte = screenData[index * 2 + 1];

                byte blink = (byte)(colorByte >> 7);
                Color backColor = Conversions.TextColors[(byte)((byte)(colorByte << 1) >> 5)]; // Bits 4-6 (discard 7)
                Color foreColor = Conversions.TextColors[(byte)((byte)(colorByte << 4) >> 4)]; // Bits 0-3                    

                m_characters[index] = Convert.ToChar(Conversions.UnicodeByteMappings[textByte]);
                m_foregroundColors[index] = foreColor;
                m_backgroundColors[index] = backColor;
                m_blink[index] = blink != 0;

                HasBlink |= (blink != 0);
            }
        }

        public Graphics.Image GenerateImage(byte[] fontData, int pixelHeight, bool blink)
        {
            using (MemoryStream fontDataStream = new MemoryStream(fontData))
            {
                FontCollection fontCollection = new();
                FontFamily consoleFontFamily = fontCollection.Add(fontDataStream);
                Font consoleFont = consoleFontFamily.CreateFont(pixelHeight / m_height); // Use whatever pixel value fits all the lines
                RichTextOptions textOptions = new(consoleFont);
                // Assume we are using a monospace font, so all upper-case chars have the same effective dimensions.  
                // We're intentionally going to pack characters just a little too close together, so that any "block" characters don't end up with 
                // fine lines in between.
                FontRectangle dimensions = TextMeasurer.MeasureAdvance("A", textOptions);
                float charHeight = dimensions.Height - 1;
                float charWidth = dimensions.Width - 1;

                float xOffset = 0, yOffset = 0;
                using (Image<Argb32> bitmap = new Image<Argb32>((int)charWidth * m_width, pixelHeight))
                {
                    bitmap.Mutate(ctx =>
                    {
                        int index = 0;
                        for (int row = 0; row < m_height; row++)
                        {
                            xOffset = 0;
                            for (int column = 0; column < m_width; column++)
                            {
                                Color foregroundColor = m_foregroundColors[index];
                                Color backgroundColor = m_backgroundColors[index];
                                char textCharacter = m_characters[index];
                                bool characterBlinking = m_blink[index];

                                ctx.FillPolygon(
                                    backgroundColor,
                                    new PointF(xOffset, yOffset),
                                    new PointF(xOffset + charWidth, yOffset),
                                    new PointF(xOffset + charWidth, yOffset + charHeight),
                                    new PointF(xOffset, yOffset + charHeight));

                                if (!(characterBlinking && blink))
                                {
                                    ctx.DrawText($"{textCharacter}", consoleFont, foregroundColor, new PointF() { X = xOffset, Y = yOffset });
                                }
                                xOffset += charWidth;

                                index++;
                            }
                            yOffset += charHeight;
                        }
                    });

                    byte[] argbData = new byte[bitmap.Height * bitmap.Width * 4];
                    int offset = 0;
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        Span<Argb32> pixelRow = bitmap.DangerousGetPixelRowMemory(y).Span;
                        foreach (ref Argb32 pixel in pixelRow)
                        {
                            argbData[offset] = pixel.A;
                            argbData[offset + 1] = pixel.R;
                            argbData[offset + 2] = pixel.G;
                            argbData[offset + 3] = pixel.B;
                            offset += 4;
                        }
                    }

                    return Graphics.Image.FromArgbBytes((bitmap.Width, bitmap.Height), argbData)!;
                }
            }
        }
    }
}
