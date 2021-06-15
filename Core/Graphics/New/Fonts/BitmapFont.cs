using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Helion.Geometry.Vectors;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Fonts.Definition;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.New.Fonts
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
                Dictionary<char, IImage> charImages = GetCharacterImages(definition, archiveCollection);
                if (charImages.Empty())
                    return null;
                
                // For now, if we have all ARGB and get a Palette, or vice-versa,
                // we will disallow this. In the future if we want, we can convert
                // it all to ARGB.
                Type type = charImages.Values.First().GetType();
                if (NotAllSameImageType(type, charImages))
                    return null;

                Func<int, int, IImage> transparentAllocator = GetTransparentAllocatorOrThrow(type);
                int maxHeight = CalculateMaxHeight(charImages);
                AddSpaceGlyphIfMissing(charImages, definition, maxHeight, transparentAllocator);

                Action<IImage, IImage> imageWriter = GetImageWriterOrThrow(type);
                var (glyphs, image) = CreateGlyphs(charImages, definition, maxHeight);
                return new Font(definition.Name, glyphs, image);
            }
            catch
            {
                return null;
            }
        }

        private static Func<int, int, IImage> GetTransparentAllocatorOrThrow(Type type)
        {
            if (type == typeof(ArgbImage))
                return (w, h) => new ArgbImage((w, h), Color.Transparent, ResourceNamespace.Fonts);
            
            if (type == typeof(PaletteImage))
                return (w, h) => new PaletteImage((w, h), PaletteImage.TransparentIndex, Vec2I.Zero, ResourceNamespace.Fonts);

            throw new Exception($"Unknown font image transparency allocator type: {type.FullName}");
        }
        
        private static Action<IImage, IImage> GetImageWriterOrThrow(Type type)
        {
            if (type == typeof(ArgbImage))
            {
                return (src, dest) =>
                {
                    if (src is not ArgbImage source || dest is not ArgbImage destination)
                        throw new Exception("Unexpected image type: {}");
                };

            }

            throw new Exception($"Unknown font image writer type: {type.FullName}");
        }

        private static int CalculateMaxHeight(Dictionary<char, IImage> charImages)
        {
            return charImages.Values.Select(i => i.Height).Max();
        }

        private static bool NotAllSameImageType(Type type, Dictionary<char, IImage> charImages)
        {
            return charImages.Values.All(i => i.GetType() == type);
        }

        private static void AddSpaceGlyphIfMissing(Dictionary<char, IImage> charImages, FontDefinition definition,
            int maxHeight, Func<int, int, IImage> transparentImageAllocatorFunc)
        {
            if (charImages.ContainsKey(' '))
                return;

            Precondition(definition.SpaceWidth != null, "Invalid definition detected, has no space image nor spacing attribute");
            int width = definition.SpaceWidth ?? 1;
            
            charImages[' '] = transparentImageAllocatorFunc(width, maxHeight);
        }

        private static Dictionary<char, IImage> GetCharacterImages(FontDefinition definition,
            ArchiveCollection archiveCollection)
        {
            Dictionary<char, IImage> charImages = new();

            foreach ((char c, CharDefinition charDef) in definition.CharDefinitions)
            {
                // TODO
                // Image? image = imageRetriever.Get(charDef.ImageName, ResourceNamespace.Graphics);
                // if (image != null)
                    // charImages[c] = new GlyphPrototype(image, charDef.Alignment);
            }

            return charImages;
        }

        private static (Dictionary<char, Glyph>, IImage) CreateGlyphs(Dictionary<char, IImage> charImages, 
            FontDefinition definition, int maxHeight)
        {
            Dictionary<char, Glyph> glyphs = new();
            
            // MSDN says "The order in which the items are returned is undefined"
            // for dictionaries, so we want a repeatable order for doing separate
            // passes while requiring the same order.

            int offsetX = 0;
            int width = charImages.Values.Select(i => i.Width).Sum();
            
            foreach ((char c, IImage charImage) in charImages)
            {
                // TODO

                offsetX += charImage.Width;
            }

            // foreach ((char c, GlyphPrototype glyphPrototype) in charImages)
            // {
            //     FontAlignment alignment = definition.Alignment;
            //     if (glyphPrototype.Alignment != null)
            //         alignment = glyphPrototype.Alignment.Value;
            //
            //     Glyph glyph = CreateGlyph(c, glyphPrototype.Image, maxHeight, alignment, definition.Grayscale);
            //     glyphs.Add(glyph);
            // }

            // TODO
            IImage atlas = null!; //CreateAtlas();
        
            return (glyphs, atlas);
        }

        // private static Glyph CreateGlyph(char c, Image image, int maxHeight, FontAlignment alignment)
        // {
        //     Precondition(maxHeight >= image.Height, "Miscalculated max height when making font");
        //
        //     int startY = 0;
        //     switch (alignment)
        //     {
        //     case FontAlignment.Top:
        //         // We're done, the default value is correct already.
        //         break;
        //     case FontAlignment.Center:
        //         startY = (maxHeight / 2) - (image.Height / 2);
        //         break;
        //     case FontAlignment.Bottom:
        //         startY = maxHeight - image.Height;
        //         break;
        //     default:
        //         throw new ArgumentOutOfRangeException(nameof(alignment), alignment, "Unexpected font alignment in glyph creation");
        //     }
        //
        //     // TODO
        //     // Image glyphImage = new Image(image.Width, maxHeight, Color.Transparent);
        //     image.DrawOnTopOf(glyphImage, (0, startY));
        //
        //     // TODO
        //     // return new Glyph(c, glyphImage);
        // }
    }
}
