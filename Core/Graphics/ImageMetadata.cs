using Helion.Geometry.Vectors;
using Helion.Resources;

namespace Helion.Graphics
{
    /// <summary>
    /// Contains extra data for an image.
    /// </summary>
    /// <remarks>
    /// This class was created to avoid the constructor turning into a giant
    /// mess. Later on we will get more and more fields and this is the best
    /// way to encapsulate them.
    /// </remarks>
    public class ImageMetadata
    {
        /// <summary>
        /// The offset of the image. These are offsets for the engine to apply
        /// to images when rendering.
        /// </summary>
        public Vec2I Offset { get; }

        /// <summary>
        /// The namespace this image was located in.
        /// </summary>
        public ResourceNamespace Namespace { get; }

        public ImageMetadata() : this(Vec2I.Zero, ResourceNamespace.Global)
        {
        }

        public ImageMetadata(Vec2I offset, ResourceNamespace resourceNamespace)
        {
            Offset = offset;
            Namespace = resourceNamespace;
        }

        public ImageMetadata(ResourceNamespace resourceNamespace)
        {
            Offset = Vec2I.Zero;
            Namespace = resourceNamespace;
        }
    }
}
