using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Helion.Resource;
using Helion.Resource.Definitions.Fonts.Definition;
using Helion.Resource.Textures;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Vectors;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.Fonts
{
    /// <summary>
    /// A helper class for composing fonts from a definitions and images.
    /// </summary>
    public static class FontCompiler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Takes a font definition and image retriever, and compiles a font.
        /// </summary>
        /// <param name="definition">The definition to make the font from.
        /// </param>
        /// <param name="resources">The object that retrieves images for
        /// the fonts.</param>
        /// <returns>A new font from the image.</returns>
        public static Font? From(FontDefinition definition, Resources resources)
        {
            if (!definition.IsValid())
                return null;

            Dictionary<char, GlyphPrototype>? charImages = GetCharacterImages(definition, resources);
            if (charImages == null)
                return null;

            FontMetrics metrics = CalculateMetrics(charImages);
            AddSpaceGlyphIfMissing(charImages, metrics, definition);

            char defaultChar = definition.CharDefinitions.First(def => def.Value.Default).Value.Character;
            if (MissingDefaultImage(defaultChar, charImages))
            {
                Log.Error("Missing default image from font {0}, cannot make font", definition.Name);
                return null;
            }

            (Glyph defaultGlyph, List<Glyph> glyphs) = CreateGlyphs(charImages, definition, metrics, defaultChar);
            return new Font(defaultGlyph, glyphs, metrics);
        }

        private static void AddSpaceGlyphIfMissing(IDictionary<char, GlyphPrototype> charImages, FontMetrics metrics,
            FontDefinition definition)
        {
            if (charImages.ContainsKey(' '))
                return;

            Precondition(definition.SpaceWidth != null, "Invalid definition detected, has no space image nor spacing attribute");
            int width = definition.SpaceWidth ?? 1;
            int height = metrics.MaxHeight;

            Image spaceImage = new Image(width, height, Color.Transparent);
            charImages[' '] = new GlyphPrototype(spaceImage, definition.Alignment);
        }

        private static Dictionary<char, GlyphPrototype>? GetCharacterImages(FontDefinition definition,
            Resources resources)
        {
            Dictionary<char, GlyphPrototype> charImages = new();

            foreach ((char c, CharDefinition charDef) in definition.CharDefinitions)
            {
                Texture texture = resources.Textures.Get(charDef.ImageName, Namespace.Graphics);
                if (!texture.IsMissing)
                    charImages[c] = new GlyphPrototype(texture.Image, charDef.Alignment);
            }

            return charImages.Empty() ? null : charImages;
        }

        private static bool MissingDefaultImage(char defaultChar, Dictionary<char, GlyphPrototype> charImages)
        {
            return !charImages.ContainsKey(defaultChar);
        }

        private static FontMetrics CalculateMetrics(Dictionary<char, GlyphPrototype> charImages)
        {
            int maxHeight = charImages.Max(pair => pair.Value.Image.Height);
            return new FontMetrics(maxHeight, maxHeight, 0, 0, 0);
        }

        private static (Glyph, List<Glyph>) CreateGlyphs(Dictionary<char, GlyphPrototype> charImages,
            FontDefinition definition, FontMetrics metrics, char defaultChar)
        {
            Glyph? defaultGlyph = null;
            List<Glyph> glyphs = new();

            foreach ((char c, GlyphPrototype glyphPrototype) in charImages)
            {
                FontAlignment alignment = definition.Alignment;
                if (glyphPrototype.Alignment != null)
                    alignment = glyphPrototype.Alignment.Value;

                Glyph glyph = CreateGlyph(c, glyphPrototype.Image, metrics.MaxHeight, alignment, definition.Grayscale);
                glyphs.Add(glyph);

                if (c == defaultChar)
                    defaultGlyph = glyph;
            }

            if (defaultGlyph == null)
                throw new NullReferenceException("Should never fail to find the default character at this point");
            return (defaultGlyph, glyphs);
        }

        private static Glyph CreateGlyph(char c, Image image, int maxHeight, FontAlignment alignment,
            bool isGrayscale)
        {
            Precondition(maxHeight >= image.Height, "Miscalculated max height when making font");

            int startY = 0;
            switch (alignment)
            {
            case FontAlignment.Top:
                // We're done, the default value is correct already.
                break;
            case FontAlignment.Center:
                startY = (maxHeight / 2) - (image.Height / 2);
                break;
            case FontAlignment.Bottom:
                startY = maxHeight - image.Height;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(alignment), alignment, "Unexpected font alignment in glyph creation");
            }

            if (isGrayscale)
                image = image.ToBrightnessCopy();

            Image glyphImage = new(image.Width, maxHeight, Color.Transparent);
            image.DrawOnTopOf(glyphImage, new Vec2I(0, startY));

            return new Glyph(c, glyphImage);
        }
    }
}