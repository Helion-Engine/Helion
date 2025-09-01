using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Graphics.Palettes;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Texture;
using NLog;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Image = Helion.Graphics.Image;

namespace Helion.Resources.Images;

/// <summary>
/// Performs image retrieval from an archive collection.
/// </summary>
public class ArchiveImageRetriever(ArchiveCollection archiveCollection, bool findNearestPaletteIndex) : IImageRetriever
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ArchiveCollection m_archiveCollection = archiveCollection;
    private readonly ResourceTracker<Image> m_compiledImages = new();
    private readonly bool m_findNearestPaletteIndex = findNearestPaletteIndex;

    public static bool IsPng(byte[] data)
    {
        return data.Length > 8 && data[0] == 137 && data[1] == 'P' && data[2] == 'N' && data[3] == 'G';
    }

    public static bool IsJpg(byte[] data)
    {
        return data.Length > 10 && data[0] == 0xFF && data[1] == 0xD8;
    }

    public static bool IsBmp(byte[] data)
    {
        return data.Length > 14 && data[0] == 'B' && data[1] == 'M';
    }

    public Image? Get(string name, ResourceNamespace priorityNamespace, GetImageOptions options = GetImageOptions.Default)
    {
        Image? compiledImage = m_compiledImages.Get(name, priorityNamespace);
        if (compiledImage != null)
            return compiledImage;

        Entry? entry = m_archiveCollection.Entries.FindByNamespace(name, priorityNamespace);
        if (entry != null)
            return ImageFromEntry(entry);

        TextureDefinition? definition = m_archiveCollection.Definitions.Textures.Get(name, priorityNamespace);
        if (definition != null)
            return ImageFromDefinition(definition);

        return null;
    }

    /// <summary>
    /// Returns a list of all image names.
    /// This creates a new list.
    /// </summary>
    /// <returns>All image names, optionally filtered to a specific namespace</returns>
    public IEnumerable<string> GetNames(ResourceNamespace specificNamespace)
    {
        return m_compiledImages.GetNames(specificNamespace)
            .Concat(m_archiveCollection.Entries.GetNames(specificNamespace))
            .Concat(m_archiveCollection.Definitions.Textures.GetNames(specificNamespace))
            .ToList();
    }

    public Image? GetOnly(string name, ResourceNamespace targetNamespace, GetImageOptions options = GetImageOptions.Default) =>
        GetOnlyMapped(name, name, targetNamespace, null, options);

    public Image? GetOnlyMapped(string mappedName, string entryName, ResourceNamespace targetNamespace, byte[]? colorTranslation, GetImageOptions options = GetImageOptions.Default)
    {
        Image? compiledImage = m_compiledImages.GetOnly(mappedName, targetNamespace);
        if (compiledImage != null)
            return compiledImage;

        TextureDefinition? definition = m_archiveCollection.Definitions.Textures.GetOnly(mappedName, targetNamespace);
        if (definition != null)
            return ImageFromDefinition(definition, options);

        Entry? entry = m_archiveCollection.Entries.FindByNamespace(entryName, targetNamespace);
        return entry != null ? ImageFromEntry(entry, colorTranslation: colorTranslation) : null;
    }

    private Image ImageFromDefinition(TextureDefinition definition, GetImageOptions options = default, byte[]? colorTranslation = null)
    {
        Image image;
        if (definition.IsAutoImageTexture)
        {
            Image? findImage = null;
            if (definition.Components.Count > 0)
            {
                var entry = m_archiveCollection.Entries.FindByNamespace(definition.Components[0].Name, definition.Namespace);
                if (entry != null)
                    findImage = ImageFromEntry(entry, cacheEntry: false, options, colorTranslation: colorTranslation);
            }

            image = findImage ?? new Image(0, 0, ImageType.PaletteWithArgb, (0, 0), definition.Namespace);
        }
        else
        {
            (int w, int h) = definition.Dimension;
            image = new(w, h, ImageType.PaletteWithArgb, (0, 0), definition.Namespace);

            foreach (TextureDefinitionComponent component in definition.Components)
            {
                Image? subImage = null;
                Entry? entry = m_archiveCollection.Entries.FindByNamespace(component.Name, definition.Namespace);

                if (entry != null)
                    subImage = ImageFromEntry(entry, cacheEntry: false, options, colorTranslation: colorTranslation);

                if (subImage == null)
                {
                    Log.Warn("Cannot find sub-image {0} when making image {1}, resulting will be corrupt", component.Name, definition.Name);
                    continue;
                }

                subImage.DrawOnTopOf(image, component.Offset);
            }
        }

        if (definition.Namespace == ResourceNamespace.Sprites)
            SetSpriteOffset(image);

        m_compiledImages.Insert(definition.Name, ResourceNamespace.Textures, image);
        return image;
    }

    private static void SetSpriteOffset(Image image)
    {
        int blankRowsFromBottom = GetBlankRowsFromBottom(image);
        if (blankRowsFromBottom <= image.Dimension.Height && blankRowsFromBottom >= 0)
            image.BlankRowsFromBottom = blankRowsFromBottom;

        int blankRowsFromTop = GetBlankRowsFromTop(image);
        if (blankRowsFromTop <= image.Dimension.Height && blankRowsFromTop >= 0)
            image.BlankRowsFromTop = blankRowsFromTop;

    }
    private static int GetBlankRowsFromTop(Image image)
    {
        if (image.ImageType != ImageType.Argb && image.ImageType != ImageType.PaletteWithArgb)
            return 0;

        bool done = false;
        int y = 0;
        for (; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                // Did we find a row that has a non-blank pixel?
                if (image.GetPixel(x, y).A != 0)
                {
                    done = true;
                    break;
                }
            }

            if (done)
                break;
        }

        return Math.Max(0, y);
    }

    private static int GetBlankRowsFromBottom(Image image)
    {
        if (image.ImageType != ImageType.Argb && image.ImageType != ImageType.PaletteWithArgb)
            return 0;

        bool done = false;
        int y = image.Height - 1;
        for (; y >= 0; y--)
        {
            for (int x = 0; x < image.Width; x++)
            {
                // Did we find a row that has a non-blank pixel?
                if (image.GetPixel(x, y).A != 0)
                {
                    done = true;
                    break;
                }
            }

            if (done)
                break;
        }

        // Return either the bottom row, or 0 if the entire image is transparent.
        return Math.Max(0, image.Height - y - 1);
    }

    // Forces helion graphics like the options background, and brightmaps, to always be true color
    private static bool AlwaysTrueColor(Entry entry) =>
        entry.Path.Name.StartsWith("helion", StringComparison.Ordinal) || entry.Namespace == ResourceNamespace.Brightmaps;

    private Image? ImageFromEntry(Entry entry, bool cacheEntry = true, GetImageOptions options = GetImageOptions.Default, byte[]? colorTranslation = null)
    {
        Image? image = null;
        byte[] data = entry.ReadData();
        bool isPng = IsPng(data);
        if (isPng || IsBmp(data) || IsJpg(data))
        {
            try
            {
                using var inputStream = new MemoryStream(data);
                using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(inputStream);
                Vec2I offset = default;
                if (isPng)
                    offset = PngChunk.GetPngOffset(new BinaryReader(inputStream));

                Palette? paletteTransltion = null;
                PaletteColorLookup? paletteTranslationColorLookup = null;
                if (m_findNearestPaletteIndex && !AlwaysTrueColor(entry))
                {
                    paletteTransltion = m_archiveCollection.Palette;
                    paletteTranslationColorLookup = m_archiveCollection.PaletteColorLookup;
                }
                // Only search for nearest palette colors if colorTranslation is set (e.g. for blood colors)
                else if (!m_findNearestPaletteIndex && !AlwaysTrueColor(entry) && colorTranslation != null)
                {
                    paletteTransltion = m_archiveCollection.Palette;
                    paletteTranslationColorLookup = m_archiveCollection.PaletteColorLookup;
                }

                image = Image.FromImageSharp(img, offset, entry.Namespace, paletteTransltion, paletteTranslationColorLookup, colorTranslation);
            }
            catch
            {
                return null;
            }
        }
        else
        {
            bool clearBlackPixels = (options & GetImageOptions.ClearBlackPixels) != 0;
            var dataEntries = m_archiveCollection.Data;
            var storeIndices = ArchiveCollection.StoreImageIndices || m_archiveCollection.Definitions.RetroBrightmapsDefinition != null;
            var palette = entry.Parent.TranslationPalette ?? dataEntries.Palette;
            
            if (colorTranslation == null && palette.Translation != null)
                colorTranslation = palette.Translation;

            if (entry.Namespace == ResourceNamespace.Flats && PaletteReaders.LikelyFlat(data))
            {
                if (PaletteReaders.ReadFlat(data, entry.Namespace, out var paletteImage))
                {
                    if (storeIndices && colorTranslation != null)
                        TranslatePaletteIndices(paletteImage.Indices, colorTranslation);
                    image = Image.PaletteToArgb(paletteImage, palette, dataEntries.Colormap.FullBright, storeIndices, clearBlackPixels, colorTranslation);
                }
            }
            else
            {
                if (PaletteReaders.ReadColumn(data, entry.Namespace, out var paletteImage))
                {
                    if (storeIndices && colorTranslation != null)
                        TranslatePaletteIndices(paletteImage.Indices, colorTranslation);
                    image = Image.PaletteToArgb(paletteImage, palette, dataEntries.Colormap.FullBright, storeIndices, clearBlackPixels, colorTranslation);
                }
            }
        }

        if (image == null)
            return null;

        if (entry.Namespace == ResourceNamespace.Sprites)
            SetSpriteOffset(image);

        if (cacheEntry)
            m_compiledImages.Insert(entry.Path.Name, entry.Namespace, image);
        return image;
    }

    private static void TranslatePaletteIndices(ushort[] indices, byte[] translation)
    {
        for (int i = 0; i < indices.Length; i++)
        {
            var value = indices[i];
            if (value == Image.TransparentIndex)
                continue;
            indices[i] = translation[value];
        }
    }

    public void Add(string name, ResourceNamespace resourceNamespace, Image image) =>
        m_compiledImages.Insert(name, resourceNamespace, image);
}
