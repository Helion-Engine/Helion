using System;
using System.IO;
using Helion.Geometry;
using Helion.Graphics;
using Helion.Graphics.Palettes;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Texture;
using Helion.Util.Extensions;
using NLog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Image = Helion.Graphics.Image;

namespace Helion.Resources.Images;

/// <summary>
/// Performs image retrieval from an archive collection.
/// </summary>
public class ArchiveImageRetriever : IImageRetriever
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ArchiveCollection m_archiveCollection;
    private readonly ResourceTracker<Image> m_compiledImages = new();

    /// <summary>
    /// Creates an image reader that uses the archive collection for its
    /// image data retrieval.
    /// </summary>
    /// <param name="archiveCollection">The collection to utilize.</param>
    public ArchiveImageRetriever(ArchiveCollection archiveCollection)
    {
        m_archiveCollection = archiveCollection;
    }

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

    public Image? Get(string name, ResourceNamespace priorityNamespace)
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

    public Image? GetOnly(string name, ResourceNamespace targetNamespace)
    {
        Image? compiledImage = m_compiledImages.GetOnly(name, targetNamespace);
        if (compiledImage != null)
            return compiledImage;

        TextureDefinition? definition = m_archiveCollection.Definitions.Textures.GetOnly(name, targetNamespace);
        if (definition != null)
            return ImageFromDefinition(definition);

        Entry? entry = m_archiveCollection.Entries.FindByNamespace(name, targetNamespace);
        return entry != null ? ImageFromEntry(entry) : null;
    }

    private Image ImageFromDefinition(TextureDefinition definition)
    {
        (int w, int h) = definition.Dimension;
        Image image = new(w, h, ImageType.Argb, (0, 0), definition.Namespace);

        foreach (TextureDefinitionComponent component in definition.Components)
        {
            Image? subImage = m_compiledImages.Get(component.Name, definition.Namespace);

            if (subImage == null)
            {
                // If we have an identical name to the child patch, we have
                // to look in our entry list only because it can lead to
                // infinite recursion and a stack overflow. Lots of vanilla
                // wads do this unfortunately...
                if (component.Name.Equals(definition.Name, StringComparison.OrdinalIgnoreCase))
                {
                    Entry? entry = m_archiveCollection.Entries.FindByNamespace(component.Name, definition.Namespace);
                    if (entry != null)
                        subImage = ImageFromEntry(entry);
                }
                else
                    subImage = Get(component.Name, definition.Namespace);
            }

            if (subImage == null)
            {
                Log.Warn("Cannot find sub-image {0} when making image {1}, resulting will be corrupt", component.Name, definition.Name);
                continue;
            }

            subImage.DrawOnTopOf(image, component.Offset);
        }

        if (definition.Namespace == ResourceNamespace.Sprites)
            SetSpriteOffset(image);

        return image;
    }

    private static void SetSpriteOffset(Image image)
    {
        int blankRows = GetBlankRowsFromBottom(image);
        if (blankRows > image.Dimension.Height || blankRows < 0)
            return;

        int height = Math.Max(image.Dimension.Height - blankRows, 1);
        image.Dimension = (image.Dimension.Width, height);
    }

    private static int GetBlankRowsFromBottom(Image image)
    {
        if (image.ImageType != ImageType.Argb)
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

    private Image? ImageFromEntry(Entry entry)
    {
        Image? image = null;
        byte[] data = entry.ReadData();

        if (IsPng(data) || IsBmp(data) || IsJpg(data))
        {
            try
            {
                using MemoryStream inputStream = new MemoryStream(data);
                using Image<Rgba32> img = SixLabors.ImageSharp.Image.Load<Rgba32>(inputStream);
                Dimension dim = (img.Width, img.Height);
                byte[] argbData = new byte[dim.Area];

                int offset = 0;
                foreach (Rgba32 rgba in img.GetPixelSpan<Rgba32>())
                {
                    argbData[offset] = rgba.R;
                    argbData[offset + 1] = rgba.G;
                    argbData[offset + 2] = rgba.B;
                    argbData[offset + 3] = rgba.A;
                    offset += 4;
                }

                image = Image.FromArgbBytes(dim, argbData, (0, 0), entry.Namespace);
            }
            catch
            {
                return null;
            }
        }
        else
        {
            if (entry.Namespace == ResourceNamespace.Flats && PaletteReaders.LikelyFlat(data))
            {
                Image? flatPaletteImage = PaletteReaders.ReadFlat(data, entry.Namespace);
                if (flatPaletteImage != null)
                    image = flatPaletteImage.PaletteToArgb(m_archiveCollection.Data.Palette);
            }
            else
            {
                Image? columnPaletteImage = PaletteReaders.ReadColumn(data, entry.Namespace);
                if (columnPaletteImage != null)
                    image = columnPaletteImage.PaletteToArgb(m_archiveCollection.Data.Palette);
            }
        }

        if (image == null)
            return null;

        if (entry.Namespace == ResourceNamespace.Sprites)
            SetSpriteOffset(image);

        m_compiledImages.Insert(entry.Path.Name, entry.Namespace, image);
        return image;
    }
}
