using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics.Geometry;
using Helion.Graphics.String;
using Helion.Render.Commands.Alignment;
using Helion.Util.Extensions;

namespace Helion.Graphics.Fonts.Renderable
{
    /// <summary>
    /// A collection of render information that can be used to draw a string.
    /// </summary>
    public class RenderableString
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
        public readonly List<RenderableSentence> Sentences;

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
        public RenderableString(ColoredString str, Font font, int fontSize, TextAlign align = TextAlign.Left,
            int maxWidth = int.MaxValue)
        {
            Font = font;
            Sentences = PopulateSentences(str, font, fontSize, maxWidth);
            DrawArea = CalculateDrawArea();
            AlignTo(align);
            RecalculateGlyphLocations();
        }

        private static List<RenderableSentence> PopulateSentences(ColoredString str, Font font, int fontSize,
            int maxWidth)
        {
            double scale = (double)fontSize / font.MaxHeight;
            int currentWidth = 0;
            int currentHeight = 0;
            List<RenderableGlyph> currentSentence = new();
            List<RenderableSentence> sentences = new();

            foreach (ColoredChar c in str)
            {
                FontGlyph fontGlyph = font[c.Character];

                int endX = currentWidth + (int)(fontGlyph.Location.Width * scale);
                int endY = currentHeight + (int)(fontGlyph.Location.Height * scale);
                Vec2I endLocation = new Vec2I(endX, endY);

                // We want to make sure each sentence has one character. This
                // also avoids infinite looping cases like a max width that is
                // too small.
                if (endLocation.X > maxWidth && !currentSentence.Empty())
                {
                    CreateAndAddSentenceIfPossible();
                    continue;
                }

                // We use a dummy box temporarily, and calculate it at the end
                // properly (for code clarity reasons).
                ImageBox2I drawLocation = new(currentWidth, currentHeight, endLocation.X, endLocation.Y);
                RenderableGlyph glyph = new(c.Character, drawLocation, ImageBox2D.ZeroToOne, fontGlyph.UV, c.Color);
                currentSentence.Add(glyph);

                currentWidth = endLocation.X;
            }

            CreateAndAddSentenceIfPossible();

            return sentences;

            void CreateAndAddSentenceIfPossible()
            {
                if (currentSentence.Empty())
                    return;

                RenderableSentence sentence = new(currentSentence);
                sentences.Add(sentence);

                currentWidth = 0;
                currentHeight += sentence.DrawArea.Height;
                currentSentence.Clear();
            }
        }

        private Dimension CalculateDrawArea()
        {
            if (Sentences.Empty())
                return default;

            // We want to pick the largest X, but sum up the Y.
            Vec2I point = Sentences
                .Select(s => s.DrawArea.Vector)
                .Aggregate((acc, area) => new Vec2I(Math.Max(acc.X, area.X), acc.Y + area.Y));

            return new(point.X, point.Y);
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
            foreach (RenderableSentence sentence in Sentences)
            {
                int gutter = (DrawArea.Width - sentence.DrawArea.Width) / 2;
                AdjustOffsetsBy(sentence, gutter);
            }
        }

        private void AlignRight()
        {
            foreach (RenderableSentence sentence in Sentences)
            {
                int gutter = DrawArea.Width - sentence.DrawArea.Width;
                AdjustOffsetsBy(sentence, gutter);
            }
        }

        private static void AdjustOffsetsBy(RenderableSentence sentence, int pixelAdjustmentWidth)
        {
            // I am afraid of ending up with copies because this is a
            // struct, so I'll do this to make sure we don't have bugs.
            //foreach (RenderedGlyph glyph in sentence.Glyphs)
            for (int i = 0; i < sentence.Glyphs.Count; i++)
            {
                RenderableGlyph glyph = sentence.Glyphs[i];

                ImageBox2I pos = glyph.Coordinates;
                ImageBox2I newCoordinate = new(pos.Left + pixelAdjustmentWidth, pos.Top, pos.Right, pos.Bottom);
                sentence.Glyphs[i] = new RenderableGlyph(glyph.Character, newCoordinate, glyph.Location, glyph.UV, glyph.Color);
            }
        }

        private void RecalculateGlyphLocations()
        {
            // It is easier to do this after the fact. Doing it during glyph
            // construction would require a ton of reading ahead, alignment,
            // and calculations which would complicate the code. Instead, we
            // do one final recalculation of the normalized coordinates here.
            Vec2D inverse = new Vec2D(1.0 / DrawArea.Width, 1.0 / DrawArea.Height);

            foreach (RenderableSentence sentence in Sentences)
            {
                for (int i = 0; i < sentence.Glyphs.Count; i++)
                {
                    RenderableGlyph glyph = sentence.Glyphs[i];

                    ImageBox2I coordinates = glyph.Coordinates;
                    Vec2D topLeft = coordinates.Min.Double * inverse;
                    Vec2D bottomRight = coordinates.Max.Double * inverse;
                    ImageBox2D location = new ImageBox2D(topLeft, bottomRight);

                    sentence.Glyphs[i] = new RenderableGlyph(glyph, location);
                }
            }
        }

        public override string ToString()
        {
            return string.Join("\n", Sentences.Select(s => s.ToString()));
        }
    }
}
