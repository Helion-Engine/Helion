using Helion.Entries;
using Helion.Graphics;
using Helion.Util;

namespace Helion.Resources.Images
{
    /// <summary>
    /// The event type that signifies what kind of action the event is.
    /// </summary>
    public enum ImageManagerEventType
    {
        CreateOrUpdate,
        Delete
    }

    /// <summary>
    /// An event that contains data about something that occurred in an image
    /// manager.
    /// </summary>
    public class ImageManagerEvent
    {
        /// <summary>
        /// The type of event this is.
        /// </summary>
        public ImageManagerEventType Type { get; }

        /// <summary>
        /// The name of the image that was affected.
        /// </summary>
        public UpperString Name { get; }

        /// <summary>
        /// The namespace of the image that was affected.
        /// </summary>
        public ResourceNamespace Namespace { get; }

        /// <summary>
        /// The actual image that was modified, or an empty value if this was a
        /// delete message.
        /// </summary>
        public Optional<Image> Image { get; }

        public ImageManagerEvent(ImageManagerEventType type, UpperString name,
            ResourceNamespace resourceNamespace, Optional<Image> image)
        {
            Type = type;
            Name = name;
            Namespace = resourceNamespace;
            Image = image;

            Assert.Postcondition(type == ImageManagerEventType.Delete || image, "Expected image value to be present for create/update");
        }

        /// <summary>
        /// A shortcut helper function to generate a create/update event.
        /// </summary>
        /// <param name="entry">The entry to create/update.</param>
        /// <param name="image">The image that was affected.</param>
        /// <returns></returns>
        public static ImageManagerEvent CreateUpdate(Entry entry, Image image)
        {
            return new ImageManagerEvent(ImageManagerEventType.CreateOrUpdate, entry.Path.Name, entry.Namespace, image);
        }

        /// <summary>
        /// A shortcut helper function to generate a create/update event.
        /// </summary>
        /// <param name="name">The name of the image.</param>
        /// <param name="resourceNamespace">The namespace of the image.</param>
        /// <param name="image">The image that was affected.</param>
        /// <returns></returns>
        public static ImageManagerEvent CreateUpdate(UpperString name, ResourceNamespace resourceNamespace, Image image)
        {
            return new ImageManagerEvent(ImageManagerEventType.CreateOrUpdate, name, resourceNamespace, image);
        }

        /// <summary>
        /// A shortcut helper function to generate a delete event.
        /// </summary>
        /// <param name="name">The entry name.</param>
        /// <param name="resourceNamespace">The resource namespace.</param>
        /// <returns></returns>
        public static ImageManagerEvent Delete(UpperString name, ResourceNamespace resourceNamespace)
        {
            return new ImageManagerEvent(ImageManagerEventType.Delete, name, resourceNamespace, Optional.Empty);
        }
    }
}
