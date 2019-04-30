using Helion.Entries.Types;
using Helion.Graphics;
using Helion.Graphics.Palette;
using Helion.Util;
using Helion.Util.Container;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Helion.Resources.Images
{
    /// <summary>
    /// A manager of images, which is responsible for tracking images, doing
    /// converting and making them available to any listeners that wish to be
    /// notified for image creation and tracking.
    /// </summary>
    public class ImageManager : IEnumerable<HashTableEntry<ResourceNamespace, UpperString, Image>>
    {
        private EventHandler<ImageManagerEventArgs> imageEventEmitter;
        public event EventHandler<ImageManagerEventArgs> ImageEventEmitter
        {
            add 
            {
                NotifyAllImages(value);
                imageEventEmitter += value;
            }
            remove { imageEventEmitter -= value; }
        }

        private readonly ResourceTracker<Image> images = new ResourceTracker<Image>();

        private void NotifyAllImages(EventHandler<ImageManagerEventArgs> eventHandler)
        {
            foreach (HashTableEntry<ResourceNamespace, UpperString, Image> tableData in this)
            {
                ResourceNamespace resourceNamespace = tableData.FirstKey;
                UpperString name = tableData.SecondKey;
                Image image = tableData.Value;

                var imageEvent = ImageManagerEventArgs.CreateUpdate(name, resourceNamespace, image);
                eventHandler(this, imageEvent);
            }
        }

        private void EmitCreateEvent(UpperString name, ResourceNamespace resourceNamespace, Image image)
        {
            var imageEvent = ImageManagerEventArgs.CreateUpdate(name, resourceNamespace, image);

            // As a reminder, this can fire when we load a project before 
            // registering any listeners.
            imageEventEmitter?.Invoke(this, imageEvent);
        }

        /// <summary>
        /// Adds an image to be tracked by the manager.
        /// </summary>
        /// <param name="entry">The image entry to track.</param>
        public void Add(ImageEntry entry)
        {
            images.AddOrOverwrite(entry.Path.Name, entry.Namespace, entry.Image);
            EmitCreateEvent(entry.Path.Name, entry.Namespace, entry.Image);
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
            images.AddOrOverwrite(entry.Path.Name, entry.Namespace, image);
            EmitCreateEvent(entry.Path.Name, entry.Namespace, image);
        }

        public IEnumerator<HashTableEntry<ResourceNamespace, UpperString, Image>> GetEnumerator()
        {
            foreach (var imageData in images)
                yield return imageData;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
