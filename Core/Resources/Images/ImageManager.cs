using Helion.Entries.Types;
using Helion.Graphics;
using Helion.Graphics.Palette;
using Helion.Resources.Definitions.Texture;
using Helion.Util;
using Helion.Util.Container;
using MoreLinq;
using NLog;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Resources.Images
{
    /// <summary>
    /// A manager of images, which is responsible for tracking images, doing
    /// converting and making them available to any listeners that wish to be
    /// notified for image creation and tracking.
    /// </summary>
    public class ImageManager : IEnumerable<HashTableEntry<ResourceNamespace, UpperString, Image>>
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Handles emitting all the events. Upon registering, any previously
        /// added images are sent.
        /// </summary>
        public event EventHandler<ImageManagerEventArgs> ImageEventEmitter
        {
            add 
            {
                NotifyAllImages(value, false);
                m_imageEventEmitter += value;
            }
            remove 
            {
                m_imageEventEmitter -= value;
            }
        }

        private EventHandler<ImageManagerEventArgs>? m_imageEventEmitter;
        private readonly ResourceTracker<Image> m_images = new ResourceTracker<Image>();

        private void NotifyAllImagesDeleted()
        {
            NotifyAllImages(null, true);
        }

        private void NotifyAllImages(EventHandler<ImageManagerEventArgs>? eventHandler, bool delete)
        {
            foreach (HashTableEntry<ResourceNamespace, UpperString, Image> tableData in this)
            {
                ResourceNamespace resourceNamespace = tableData.FirstKey;
                UpperString name = tableData.SecondKey;
                Image image = tableData.Value;

                ImageManagerEventArgs imageEvent;

                if (delete)
                    imageEvent = ImageManagerEventArgs.Delete(name, resourceNamespace);
                else
                    imageEvent = ImageManagerEventArgs.CreateUpdate(name, resourceNamespace, image);


                if (eventHandler == null)
                    m_imageEventEmitter?.Invoke(this, imageEvent);
                else
                    eventHandler(this, imageEvent);
            }
        }

        private void EmitCreateEvent(UpperString name, ResourceNamespace resourceNamespace, Image image)
        {
            var imageEvent = ImageManagerEventArgs.CreateUpdate(name, resourceNamespace, image);

            // As a reminder, this can fire when we load a project before 
            // registering any listeners.
            m_imageEventEmitter?.Invoke(this, imageEvent);
        }

        private (Image Image, UpperString Name) ImageFromTextureX(Pnames pnames, TextureXImage imageDefinition)
        {
            ImageMetadata metadata = new ImageMetadata(ResourceNamespace.Textures);
            Image image = new Image(imageDefinition.Width, imageDefinition.Height, Image.Transparent, metadata);

            foreach (TextureXPatch patch in imageDefinition.Patches)
            {
                if (patch.PnamesIndex < 0 || patch.PnamesIndex >= pnames.Names.Count)
                {
                    log.Warn("Unable to find patch index {0} for texture X definition '{1}'", patch.PnamesIndex, imageDefinition.Name);
                    continue;
                }

                UpperString patchName = pnames.Names[patch.PnamesIndex];
                Image? patchImage = m_images.GetWithGlobal(patchName, ResourceNamespace.Textures);
                if (patchImage == null)
                {
                    log.Warn("Unable to find patch '{0}' for texture X definition '{1}'", patchName, imageDefinition.Name);
                    continue;
                }

                patchImage.DrawOnTopOf(image, patch.Offset);
            }

            return (image, imageDefinition.Name);
        }

        /// <summary>
        /// Adds an image to be tracked by the manager for some image entry.
        /// </summary>
        /// <param name="entry">The image entry to track.</param>
        public void Add(ImageEntry entry) => Add(entry.Image, entry.Path.Name);

        /// <summary>
        /// Adds an image to be tracked by the manager.
        /// </summary>
        /// <param name="image">The image to track.</param>
        /// <param name="name">The name of the image.</param>
        public void Add(Image image, UpperString name)
        {
            m_images.AddOrOverwrite(name, image.Metadata.Namespace, image);
            EmitCreateEvent(name, image.Metadata.Namespace, image);
        }

        /// <summary>
        /// Will compile a series of new images from the vanilla definition
        /// entries provided.
        /// </summary>
        /// <param name="pnames">The patch name to indices.</param>
        /// <param name="textureXList">The texture definitions.</param>
        public void AddTextureDefinitions(Pnames pnames, List<TextureXImage> textureXList)
        {
            textureXList.Select(textureXImage => ImageFromTextureX(pnames, textureXImage))
                .ForEach(imagePair => Add(imagePair.Image, imagePair.Name));
        }

        /// <summary>
        /// Adds a palette image to be tracked by the image manager. It will do
        /// a conversion into an Image with the palette provided.
        /// </summary>
        /// <param name="entry">The entry to track.</param>
        /// <param name="palette">The palette to use for conversions.</param>
        public void Add(PaletteImageEntry entry, Palette palette)
        {
            Image image = entry.PaletteImage.ToImage(palette);
            m_images.AddOrOverwrite(entry.Path.Name, entry.Namespace, image);
            EmitCreateEvent(entry.Path.Name, entry.Namespace, image);
        }

        public IEnumerator<HashTableEntry<ResourceNamespace, UpperString, Image>> GetEnumerator()
        {
            foreach (var imageData in m_images)
                yield return imageData;
        }

        public void ClearImages()
        {
            NotifyAllImagesDeleted();
            foreach (var image in m_images)
                image.Value.Bitmap.Dispose();
            m_images.Clear();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
