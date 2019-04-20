using Helion.Entries;
using Helion.Entries.Types;
using Helion.Graphics;
using Helion.Graphics.Palette;
using Helion.Util;
using Helion.Util.Container;
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
        private readonly ResourceTracker<Image> images = new ResourceTracker<Image>();
        private readonly List<ImageManagerListener> listeners = new List<ImageManagerListener>();

        private void NotifyListener(Entry entry, Image image)
        {
            // Note that right now this means every image that is registered
            // will cause an event to be made. We will probably want to batch
            // them in the future.
            listeners.ForEach(l => l.HandleImageEvent(ImageManagerEvent.CreateUpdate(entry, image)));
        }

        private void NotifyAllImages(ImageManagerListener listener)
        {
            foreach (HashTableEntry<ResourceNamespace, UpperString, Image> tableData in this)
            {
                ResourceNamespace resourceNamespace = tableData.FirstKey;
                UpperString name = tableData.SecondKey;
                Image image = tableData.Value;

                ImageManagerEvent imageEvent = ImageManagerEvent.CreateUpdate(name, resourceNamespace, image);
                listener.HandleImageEvent(imageEvent);
            }
        }

        /// <summary>
        /// Adds an image to be tracked by the manager.
        /// </summary>
        /// <param name="entry">The image entry to track.</param>
        public void Add(ImageEntry entry)
        {
            images.AddOrOverwrite(entry.Path.Name, entry.Namespace, entry.Image);
            NotifyListener(entry, entry.Image);
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
            NotifyListener(entry, image);
        }

        /// <summary>
        /// Registers a listener to be notified on any image create, update, or
        /// delete events. 
        /// </summary>
        /// <remarks>
        /// This will also update it with all the images that currently are 
        /// registered, so after this function returns then the listener is all
        /// updated.
        /// </remarks>
        /// <param name="listener">The listener to register. Should only be
        /// registered once.</param>
        public void Register(ImageManagerListener listener)
        {
            if (listeners.Contains(listener))
                Assert.Fail($"Trying to add the same image manager listener twice: {listener}");
            else
            {
                listeners.Add(listener);
                NotifyAllImages(listener);
            }
        }

        /// <summary>
        /// Unregisters a listener. Does nothing if not registered.
        /// </summary>
        /// <param name="listener">The listener to unregister.</param>
        public void Unregister(ImageManagerListener listener)
        {
            listeners.Remove(listener);
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
