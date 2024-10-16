using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Fonts.Definition;
using Helion.Resources.Images;
using Helion.Util.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.Fonts;

public static class BitmapFont
{
    private static readonly char[] NumberChars = [' ', '+', '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];

    /// <summary>
    /// Reads a bitmap font.
    /// </summary>
    /// <param name="definition">The font definition.</param>
    /// <param name="archiveCollection">The source of the images.</param>
    /// <returns>The font, or null if it cannot be made.</returns>
    public static Font? From(FontDefinition definition, ArchiveCollection archiveCollection, int scale)
    {
        if (!definition.IsValid())
            return null;

        try
        {
            Dictionary<char, Image> charImages = GetCharacterImages(definition, archiveCollection,
                out int maxHeight, out int? numberFixedWidth, out ImageType imageType);

            if (charImages.Empty())
                return null;

            AddSpaceGlyphIfMissing(charImages, definition, maxHeight, imageType);
            var (glyphs, image) = CreateGlyphs(definition, charImages, maxHeight, numberFixedWidth, imageType, scale);

            // SmallGrayscaleFont has colors applied and needs to be full color to support different colors.
            if (definition.Grayscale)
            {
                image.ConvertToGrayscale(definition.GrayscaleNormalization);
                image.DisableIndexedUpload();
            }

            if (scale > 1)
            {
                // If we've upscaled the font, it has interpolated colors in it now, and doesn't follow the palette.
                image.DisableIndexedUpload();
            }

            return new Font(definition.Name, glyphs, image, fixedWidth: definition.FixedWidth, fixedHeight: definition.FixedHeight, fixedWidthChar: definition.FixedWidthChar, scale: scale);
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
        int height = definition.FixedHeight ?? maxHeight;

        charImages[' '] = new Image(width, height, imageType);
    }

    private static Dictionary<char, Image> GetCharacterImages(FontDefinition definition,
        ArchiveCollection archiveCollection, out int maxHeight, out int? numberFixedWidth, out ImageType imageType)
    {
        imageType = ImageType.Argb;
        maxHeight = 0;
        numberFixedWidth = null;
        Dictionary<char, Image> charImages = new();

        // TODO: TEMPORARY: The texture manager should do all of this for us later on!
        var imageRetriever = new ArchiveImageRetriever(archiveCollection);

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

        maxHeight = definition.FixedHeight ?? CalculateMaxHeight(charImages);

        imageType = charImages.Values.First().ImageType;
        if (NotAllSameImageType(imageType, charImages))
            throw new Exception("Mixing different image types when making bitmap font");

        if (definition.FixedWidthNumber != null
            && definition.FixedWidth == null
            && charImages.TryGetValue(definition.FixedWidthNumber.Value, out Image? fixedWidthImage)
            && fixedWidthImage != null)
            numberFixedWidth = fixedWidthImage.Width;

        Dictionary<char, Image> processedCharImages = new();
        foreach ((char c, Image charImage) in charImages)
        {
            FontAlignment alignment = definition.CharDefinitions[c].Alignment ?? definition.Alignment;
            processedCharImages[c] = CreateCharImage(charImage, maxHeight, alignment, imageType);
        }

        return processedCharImages;
    }

    private static (Dictionary<char, Glyph>, Image) CreateGlyphs(FontDefinition definition, Dictionary<char, Image> charImages,
        int maxHeight, int? numberFixedWidth, ImageType imageType, int scale)
    {
        Dictionary<char, Glyph> glyphs = new();
        int atlasOffsetX = 0;
        const int padding = 1;
        int width = 0;
        if (definition.FixedWidth != null)
        {
            // All chars are fixed width
            width = (charImages.Count * definition.FixedWidth.Value) + (padding * charImages.Count * 2);
        }
        else if (numberFixedWidth != null)
        {
            // Chars are variable width except for numbers
            width = charImages.Where(c => !NumberChars.Contains(c.Key)).Select(i => i.Value.Width).Sum();
            width += numberFixedWidth.Value * NumberChars.Length;
            width += padding * charImages.Count * 2;
        }
        else
        {
            // All chars are variable width
            width = charImages.Values.Select(i => i.Width).Sum() + padding * charImages.Count * 2;
        }

        if (definition.FixedHeight != null)
            maxHeight = definition.FixedHeight.Value;

        bool canUpscale = definition.Grayscale || charImages.All(img => img.Value.ImageType == ImageType.Argb || img.Value.ImageType == ImageType.PaletteWithArgb);
        width = canUpscale ? width * scale : width;
        maxHeight = canUpscale ? maxHeight * scale : maxHeight;

        Dimension atlasDimension = (width, maxHeight);
        Image atlas = new(width, maxHeight, definition.Grayscale ? ImageType.Argb : imageType);

        foreach ((char c, Image image) in charImages)
        {
            var charImage = scale != 1 && canUpscale
                ? image.GetUpscaled(scale, ResourceNamespace.Fonts)
                : image;
            atlasOffsetX += padding;


            int? numberWidth = numberFixedWidth != null && NumberChars.Contains(c)
                ? numberFixedWidth * scale
                : null;

            int charWidth = definition.FixedWidth * scale ?? numberWidth ?? charImage.Width;
            if (numberWidth != null)
            {
                // Center-align within a fixed-width cell
                charImage.DrawOnTopOf(atlas, (atlasOffsetX + (charWidth - charImage.Width) / 2, 0));
            }
            else
            {
                charImage.DrawOnTopOf(atlas, (atlasOffsetX, 0));
            }

            var glyphDimension = charImage.Dimension;
            glyphDimension.Width = charWidth;
            var offset = definition.UseOffset ? new Vec2I(charImage.Offset.X, -charImage.Offset.Y) : Vec2I.Zero;

            if (definition.FixedWidth != null && charImage.Width < definition.FixedWidth.Value)
                offset.X += definition.FixedWidth.Value - charImage.Width;
            // Offsets need to handled in the renderer as they can be drawn off atlas.
            glyphs[c] = new Glyph(c, (atlasOffsetX, 0), glyphDimension, atlasDimension, offset);
            atlasOffsetX += charWidth + padding;
        }

        return (glyphs, atlas);
    }

    private static Image CreateCharImage(Image image, int maxHeight, FontAlignment alignment, ImageType imageType)
    {
        Precondition(maxHeight >= image.Height, "Miscalculated max height when making font");

        if (image.Height == maxHeight)
            return image;

        int startY = alignment switch
        {
            FontAlignment.Top => 0,
            FontAlignment.Center => (maxHeight / 2) - (image.Height / 2),
            FontAlignment.Bottom => maxHeight - image.Height,
            _ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, "Unexpected font alignment in glyph creation"),
        };
        Image glyphImage = new(image.Width, maxHeight, imageType, offset: image.Offset);
        image.DrawOnTopOf(glyphImage, (0, startY));

        return glyphImage;
    }
}
