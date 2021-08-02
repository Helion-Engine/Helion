using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Fonts.Definition;
using Helion.Resources.Images;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.Fonts
{
    public static class BitmapFont
    {
        /// <summary>
        /// Reads a bitmap font.
        /// </summary>
        /// <param name="definition">The font definition.</param>
        /// <param name="archiveCollection">The source of the images.</param>
        /// <returns>The font, or null if it cannot be made.</returns>
        public static Font? From(FontDefinition definition, ArchiveCollection archiveCollection)
        {
            if (!definition.IsValid())
                return null;

            try
            {
                Dictionary<char, Image> charImages = GetCharacterImages(definition, archiveCollection,
                    out int maxHeight, out ImageType imageType);
                
                if (charImages.Empty())
                    return null;
                
                // For now, if we have all ARGB and get a Palette, or vice-versa,
                // we will disallow this. In the future if we want, we can convert
                // it all to ARGB.
                // TODO

                AddSpaceGlyphIfMissing(charImages, definition, maxHeight, imageType);
                
                var (glyphs, image) = CreateGlyphs(charImages, maxHeight, imageType);
                return new Font(definition.Name, glyphs, image);
            }
            catch
            {
                return null;
            }
        }

        private static int CalculateMaxHeight(Dictionary<char, Image> charImages)
        {
            return charImages.Values.Select(i => i.Height).Max();
        }

        private static bool NotAllSameImageType(ImageType type, Dictionary<char, Image> charImages)
        {
            return charImages.Values.Any(i => i.ImageType != type);
        }

        private static void AddSpaceGlyphIfMissing(Dictionary<char, Image> charImages, FontDefinition definition,
            int maxHeight, ImageType imageType)
        {
            if (charImages.ContainsKey(' '))
                return;

            Precondition(definition.SpaceWidth != null, "Invalid definition detected, has no space image nor spacing attribute");
            int width = definition.SpaceWidth ?? 1;
            
            charImages[' '] = new Image(width, maxHeight, imageType);
        }

        private static Dictionary<char, Image> GetCharacterImages(FontDefinition definition,
            ArchiveCollection archiveCollection, out int maxHeight, out ImageType imageType)
        {
            imageType = ImageType.Argb;
            maxHeight = 0;
            
            Dictionary<char, Image> charImages = new();
            
            // TODO: TEMPORARY: The texture manager should do all of this for us later on!
            IImageRetriever imageRetriever = new ArchiveImageRetriever(archiveCollection);

            // Unfortunately we need to know the max height, and require all of
            // the images beforehand to make such a calculation.
            foreach ((char c, CharDefinition charDef) in definition.CharDefinitions)
            {
                Image? image = imageRetriever.Get(charDef.ImageName, ResourceNamespace.Graphics);
                if (image != null)
                    charImages[c] = image;
            }

            if (charImages.Empty())
                return new Dictionary<char, Image>();

            maxHeight = CalculateMaxHeight(charImages);
            
            imageType = charImages.Values.First().ImageType;
            if (NotAllSameImageType(imageType, charImages))
                throw new Exception("Mixing different image types when making bitmap font");

            Dictionary<char, Image> processedCharImages = new();
            foreach ((char c, Image charImage) in charImages)
                processedCharImages[c] = CreateCharImage(charImage, maxHeight, definition.Alignment, imageType);
            return processedCharImages;
        }

        private static (Dictionary<char, Glyph>, Image) CreateGlyphs(Dictionary<char, Image> charImages, 
            int maxHeight, ImageType imageType)
        {
            Dictionary<char, Glyph> glyphs = new();
            int offsetX = 0;
            int width = charImages.Values.Select(i => i.Width).Sum();

            Dimension atlasDimension = (width, maxHeight);
            Image atlas = new(width, maxHeight, imageType);
            
            foreach ((char c, Image charImage) in charImages)
            {
                charImage.DrawOnTopOf(atlas, (offsetX, 0));
                glyphs[c] = new Glyph(c, (offsetX, 0), charImage.Dimension, atlasDimension);
                offsetX += charImage.Width;
            }
        
            return (glyphs, atlas);
        }

        private static Image CreateCharImage(Image image, int maxHeight, FontAlignment alignment,
            ImageType imageType)
        {
            Precondition(maxHeight >= image.Height, "Miscalculated max height when making font");

            if (image.Height == maxHeight)
                return image;

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
        
            Image glyphImage = new(image.Width, maxHeight, imageType);
            image.DrawOnTopOf(glyphImage, (0, startY));

            return glyphImage;
        }
    }
}
