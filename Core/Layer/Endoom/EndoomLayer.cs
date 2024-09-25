namespace Helion.Layer.Endoom
{
    using Helion.Render.Common.Renderers;
    using Helion.Render.Common.Textures;
    using Helion.Render.OpenGL.Texture;
    using Helion.Resources.Archives.Collection;
    using Helion.Util.Extensions;
    using Helion.Util.Timing;
    using Helion.Window;
    using SixLabors.Fonts;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.Advanced;
    using SixLabors.ImageSharp.Drawing.Processing;
    using SixLabors.ImageSharp.PixelFormats;
    using SixLabors.ImageSharp.Processing;
    using System;
    using System.IO;

    public class EndoomLayer : IGameLayer
    {
        // The ENDOOM format follows these specifications:
        // 1. It is 4000 bytes and represents an 80x25 text block
        // 2. The bytes alternate between "letter" bytes and "color" bytes
        // 3. Letter bytes are extended ASCII code page 437 and either need to be converted to Unicode or rendered with a VGA font
        // 4. Color bytes: bits 0-3 are foreground color, 4-6 are background color, 7 is "blink"
        const int ENDOOMBYTES = 4000;
        const int ENDOOMCOLUMNS = 80;
        const int ENDOOMROWS = ENDOOMBYTES / ENDOOMCOLUMNS / 2;
        const string FONTNAME = "flexi-ibm-vga-true.regular";
        const string LUMPNAME = "ENDOOM";
        private const string IMAGENAME1 = "ENDOOM_RENDERED_1";
        private const string IMAGENAME2 = "ENDOOM_RENDERED_2";
        const int SCALE = 4;

        private readonly Action m_closeAction;
        private readonly ArchiveCollection m_archiveCollection;
        private Graphics.Image? m_endoomImage1;
        private Graphics.Image? m_endoomImage2;

        private IRenderableTextureHandle? m_texture1;
        private IRenderableTextureHandle? m_texture2;

        public EndoomLayer(Action closeAction, ArchiveCollection archiveCollection)
        {
            m_closeAction = closeAction;
            m_archiveCollection = archiveCollection;

            ComputeImages(
                m_archiveCollection.FindEntry(LUMPNAME)?.ReadData() ?? [],
                m_archiveCollection.FindEntry(FONTNAME)?.ReadData() ?? [],
                scale: 3);
        }

        public void Dispose()
        {
            m_endoomImage1 = null;
            m_endoomImage2 = null;
            (m_texture1 as GLTexture)?.Dispose();
            (m_texture2 as GLTexture)?.Dispose();
        }

        public void HandleInput(IConsumableInput input)
        {
            if (input.HasAnyKeyPressed())
            {
                m_closeAction();
            }

            input.ConsumeAll();
        }

        public void RunLogic(TickerInfo tickerInfo)
        {
            if (m_endoomImage1 == null)
            {
                m_closeAction();
            }
        }

        public virtual void Render(IHudRenderContext hud)
        {
            hud.Clear(Graphics.Color.Black);

            if (m_endoomImage1 == null || m_endoomImage2 == null)
            {
                return;
            }

            if ((DateTime.Now.Millisecond / 500) == 0) // cycle 2x/second
            {
                hud.RenderFullscreenImage(m_endoomImage1, IMAGENAME1, Resources.ResourceNamespace.Textures, out m_texture1);
            }
            else
            {
                hud.RenderFullscreenImage(m_endoomImage2, IMAGENAME2, Resources.ResourceNamespace.Textures, out m_texture2);
            }
        }

        private void ComputeImages(byte[] endoomBytes, byte[] fontBytes, int scale)
        {
            // In what might be one of the silliest performance regressions possible, we're emulating the ENDOOM screen,
            // which was originally a sequence of bytes that could be copied directly to video memory, by rendering it
            // into a bitmap.
            if (endoomBytes.Length != ENDOOMBYTES)
            {
                // Not valid ENDOOM lump
                return;
            }

            byte[] textBytes = new byte[ENDOOMBYTES / 2];
            byte[] colorBytes = new byte[ENDOOMBYTES / 2];

            for (int i = 0; i < endoomBytes.Length; i += 2)
            {
                textBytes[i / 2] = endoomBytes[i];
                colorBytes[i / 2] = endoomBytes[i + 1];
            }

            using (MemoryStream fontDataStream = new MemoryStream(fontBytes))
            {
                FontCollection fontCollection = new();
                FontFamily consoleFontFamily = fontCollection.Add(fontDataStream);
                Font consoleFont = consoleFontFamily.CreateFont(scale * 8);
                RichTextOptions textOptions = new(consoleFont);
                // Assume we are using a monospace font, so all upper-case chars have the same effective dimensions.  
                // We're intentionally going to pack characters just a little too close together, so that any "block" characters don't end up with 
                // fine lines in between.
                FontRectangle dimensions = TextMeasurer.MeasureAdvance("A", textOptions);
                float charHeight = dimensions.Height - 1;
                float charWidth = dimensions.Width - 1;

                for (int imageCount = 0; imageCount < 2; imageCount++)
                {
                    float xOffset = 0, yOffset = 0;
                    using (Image<Rgba32> endoomBitmap = new Image<Rgba32>((int)charWidth * ENDOOMCOLUMNS, (int)charHeight * ENDOOMROWS))
                    {
                        endoomBitmap.Mutate(ctx =>
                        {
                            for (int row = 0; row < ENDOOMROWS; row++)
                            {
                                xOffset = 0;
                                for (int column = 0; column < ENDOOMCOLUMNS; column++)
                                {
                                    byte colorByte = colorBytes[(row * ENDOOMCOLUMNS) + column];
                                    byte blink = (byte)(colorByte >> 7);
                                    Color backColor = Conversions.TextColors[(byte)((byte)(colorByte << 1) >> 5)]; // Bits 4-6 (discard 7)
                                    Color foreColor = blink == 0 || imageCount == 0
                                        ? Conversions.TextColors[(byte)((byte)(colorByte << 4) >> 4)] // Bits 0-3
                                        : backColor;

                                    ctx.FillPolygon(
                                        backColor,
                                        new PointF(xOffset, yOffset),
                                        new PointF(xOffset + charWidth, yOffset),
                                        new PointF(xOffset + charWidth, yOffset + charHeight),
                                        new PointF(xOffset, yOffset + charHeight));

                                    ctx.DrawText(
                                        $"{Convert.ToChar(Conversions.UnicodeByteMappings[textBytes[(row * ENDOOMCOLUMNS) + column]])}",
                                        consoleFont,
                                        foreColor,
                                        new PointF() { X = xOffset, Y = yOffset });
                                    xOffset += charWidth;
                                }
                                yOffset += charHeight;
                            }
                        });


                        byte[] argbData = new byte[endoomBitmap.Height * endoomBitmap.Width * 4];
                        int offset = 0;
                        for (int y = 0; y < endoomBitmap.Height; y++)
                        {
                            Span<Rgba32> pixelRow = endoomBitmap.DangerousGetPixelRowMemory(y).Span;
                            foreach (ref Rgba32 pixel in pixelRow)
                            {
                                argbData[offset] = pixel.A;
                                argbData[offset + 1] = pixel.R;
                                argbData[offset + 2] = pixel.G;
                                argbData[offset + 3] = pixel.B;
                                offset += 4;
                            }
                        }

                        Graphics.Image? convertedImage = Graphics.Image.FromArgbBytes((endoomBitmap.Width, endoomBitmap.Height), argbData);

                        if (imageCount == 0)
                        {
                            m_endoomImage1 = convertedImage;
                        }
                        else
                        {
                            m_endoomImage2 = convertedImage;
                        }
                    }
                }
            }
        }
    }
}
