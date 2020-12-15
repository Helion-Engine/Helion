using System.Drawing;
using System.IO;
using Helion.Graphics;
using Helion.Graphics.Palette;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Texture;
using Helion.Util;
using Helion.Util.Geometry.Vectors;
using NLog;
using Image = Helion.Graphics.Image;

namespace Helion.Resources.Images
{
    /// <summary>
    /// Performs image retrieval from an archive collection.
    /// </summary>
    public class ArchiveImageRetriever : IImageRetriever
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ArchiveCollection m_archiveCollection;
        private readonly ResourceTracker<Image> m_compiledImages = new ResourceTracker<Image>();
        
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

        /// <inheritdoc/>
        public Image? Get(CIString name, Namespace priorityNamespace)
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
        
        /// <inheritdoc/>
        public Image? GetOnly(CIString name, Namespace targetNamespace)
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
            ImageMetadata imageMetadata = new ImageMetadata(definition.Namespace);
            Image image = new Image(definition.Width, definition.Height, Color.Transparent, imageMetadata);
            
            foreach (TextureDefinitionComponent component in definition.Components)
            {
                Image? subImage = m_compiledImages.Get(component.Name, definition.Namespace);

                if (subImage == null)
                {
                    // If we have an identical name to the child patch, we have
                    // to look in our entry list only because it can lead to
                    // infinite recursion and a stack overflow. Lots of vanilla
                    // wads do this unfortunately...
                    if (component.Name == definition.Name)
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

            m_compiledImages.Insert(definition.Name, definition.Namespace, image);
            return image;
        }

        private Image? ImageFromEntry(Entry entry)
        {
            Image? image = null;
            byte[] data = entry.ReadData();

            if (IsPng(data) || IsBmp(data) || IsJpg(data))
            {
                try
                {
                    image = new Image(new Bitmap(new MemoryStream(data), true), new ImageMetadata(new Vec2I(0, 0), entry.Namespace));
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                if (PaletteReaders.LikelyFlat(data))
                {
                    PaletteImage? flatPaletteImage = PaletteReaders.ReadFlat(data, entry.Namespace);
                    if (flatPaletteImage != null)
                        image = flatPaletteImage.ToImage(m_archiveCollection.Data.Palette);
                }
                else
                {
                    PaletteImage? columnPaletteImage = PaletteReaders.ReadColumn(data, entry.Namespace);
                    if (columnPaletteImage != null)
                        image = columnPaletteImage.ToImage(m_archiveCollection.Data.Palette);
                }
            }

            if (image != null)
                m_compiledImages.Insert(entry.Path.Name, entry.Namespace, image);

            return image;
        }
    }
}