using System.Drawing;
using Helion.Graphics;
using Helion.Graphics.Palette;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Texture;
using Helion.Util;
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

        /// <inheritdoc/>
        public Image? Get(CIString name, ResourceNamespace priorityNamespace)
        {
            Image? compiledImage = m_compiledImages.Get(name, priorityNamespace);
            if (compiledImage != null)
                return compiledImage;

            TextureDefinition? definition = m_archiveCollection.Definitions.Textures.Get(name, priorityNamespace);
            if (definition != null)
                return ImageFromDefinition(definition);
            
            Entry? entry = m_archiveCollection.GetEntry(name, priorityNamespace);
            return entry != null ? ImageFromEntry(entry) : null;
        }
        
        /// <inheritdoc/>
        public Image? GetOnly(CIString name, ResourceNamespace targetNamespace)
        {
            Image? compiledImage = m_compiledImages.GetOnly(name, targetNamespace);
            if (compiledImage != null)
                return compiledImage;

            TextureDefinition? definition = m_archiveCollection.Definitions.Textures.GetOnly(name, targetNamespace);
            if (definition != null)
                return ImageFromDefinition(definition);
            
            Entry? entry = m_archiveCollection.GetEntry(name, targetNamespace);
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
                        Entry? entry = m_archiveCollection.GetEntry(component.Name, definition.Namespace);
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
            byte[] data = entry.ReadData();
            
            // TODO: If its a png/jpg/etc, read the format instead.
            // TODO: If it's a png, make sure to read the offsets from 'grAb'!

            if (PaletteReaders.LikelyFlat(data))
            {
                PaletteImage? flatPaletteImage = PaletteReaders.ReadFlat(data, entry.Namespace);
                if (flatPaletteImage != null)
                {
                    Image flatImage = flatPaletteImage.ToImage(m_archiveCollection.Data.Palette);
                    m_compiledImages.Insert(entry.Path.Name, entry.Namespace, flatImage);
                    
                    return flatImage;
                }
            }

            PaletteImage? columnPaletteImage = PaletteReaders.ReadColumn(data, entry.Namespace);
            if (columnPaletteImage == null) 
                return null;
            
            Image columnImage = columnPaletteImage.ToImage(m_archiveCollection.Data.Palette);
            m_compiledImages.Insert(entry.Path.Name, entry.Namespace, columnImage);

            return columnImage;
        }
    }
}