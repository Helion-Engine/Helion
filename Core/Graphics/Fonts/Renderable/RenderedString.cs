using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Helion.Graphics.String;
using Helion.Render.Commands.Alignment;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;

namespace Helion.Graphics.Fonts.Renderable
{
    /// <summary>
    /// A collection of render information that can be used to draw a string.
    /// </summary>
    public class RenderedString
    {
        /// <summary>
        /// The font used when rendering this.
        /// </summary>
        public readonly Font Font;

        /// <summary>
        /// The area that encapsulates all the glyphs.
        /// </summary>
        public readonly Dimension DrawArea;

        /// <summary>
        /// All the glyphs and their positions to be drawn.
        /// </summary>
        public readonly List<RenderedSentence> Sentences;

        /// <summary>
        /// Creates a rendered string that is ready to be passed to a renderer.
        /// </summary>
        /// <param name="font">The font to use.</param>
        /// <param name="str">The colored string to process.</param>
        /// <param name="fontSize">The height of the characters, in pixels. If
        /// the font has space padding on the top and bottom, those are taken
        /// into account. If you do not want that, you have to trim your fonts.
        /// </param>
        /// <param name="align">Alignment (only needed if there are multiple
        /// lines, otherwise it does not matter).</param>
        /// <param name="maxWidth">How wide before wrapping around.</param>
        public RenderedString(Font font, ColoredString str, int fontSize, TextAlign align = TextAlign.Left,
            int maxWidth = int.MaxValue)
        {
            Font = font;
            Sentences = PopulateSentences(font, str, fontSize, maxWidth);
            DrawArea = CalculateDrawArea();
            AlignTo(align);
        }

        private static List<RenderedSentence> PopulateSentences(Font font, ColoredString str, int fontSize,
            int maxWidth)
        {
            double scale = (double)fontSize / font.MaxHeight;
            int currentWidth = 0;
            int currentHeight = 0;
            List<RenderedSentence> sentences = new();
            List<RenderedGlyph> currentSentence = new();

            foreach (ColoredChar c in str)
            {
                FontGlyph fontGlyph = font[c.Character];

                Vec2D dimension = new Vec2D(fontGlyph.Location.Width, fontGlyph.Location.Height) * scale;
                Vec2I location = new Vec2I((int)(dimension.X + currentWidth), currentHeight);

                // We want to make sure each sentence has one character. This
                // also avoids infinite looping cases like a max width that is
                // too small.
                if (location.X > maxWidth && !currentSentence.Empty())
                {
                    CreateAndAddSentenceIfPossible();
                    continue;
                }

                Rectangle drawLocation = new(location.X, location.Y, (int)dimension.X, (int)dimension.Y);
                RenderedGlyph glyph = new(c.Character, drawLocation, fontGlyph.UV, c.Color);
                currentSentence.Add(glyph);

                currentWidth += location.X;
            }

            CreateAndAddSentenceIfPossible();

            return sentences;

            void CreateAndAddSentenceIfPossible()
            {
                if (currentSentence.Empty())
                    return;

                RenderedSentence sentence = new(currentSentence);
                sentences.Add(sentence);

                currentHeight += sentence.DrawArea.Height;
                currentSentence.Clear();
            }
        }

        private Dimension CalculateDrawArea()
        {
            // We want to pick the largest X, but sum up the Y.
            return Sentences
                .Select(s => s.DrawArea.ToVector())
                .Aggregate((acc, area) => new Vec2I(Math.Max(acc.X, area.X), acc.Y + area.Y))
                .ToDimension();
        }

        private void AlignTo(TextAlign align)
        {
            // If it's not any of these, then we're done.
            switch (align)
            {
            case TextAlign.Center:
                AlignCenter();
                break;
            case TextAlign.Right:
                AlignRight();
                break;
            }
        }

        private void AlignCenter()
        {
            foreach (RenderedSentence sentence in Sentences)
            {
                int gutter = (DrawArea.Width - sentence.DrawArea.Width) / 2;
                AdjustOffsetsBy(sentence, gutter);
            }
        }

        private void AlignRight()
        {
            foreach (RenderedSentence sentence in Sentences)
            {
                int gutter = DrawArea.Width - sentence.DrawArea.Width;
                AdjustOffsetsBy(sentence, gutter);
            }
        }

        private static void AdjustOffsetsBy(RenderedSentence sentence, int pixelAdjustmentWidth)
        {
            // I am afraid of ending up with copies because this is a
            // struct, so I'll do this to make sure we don't have bugs.
            //foreach (RenderedGlyph glyph in sentence.Glyphs)
            for (int i = 0; i < sentence.Glyphs.Count; i++)
            {
                RenderedGlyph glyph = sentence.Glyphs[i];

                Rectangle newLocation = glyph.Location;
                newLocation.X += pixelAdjustmentWidth;

                sentence.Glyphs[i] = new RenderedGlyph(glyph.Character, newLocation, glyph.UV, glyph.Color);
            }
        }
    }
}
