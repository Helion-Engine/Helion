using Helion.Resources;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;

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

        public ImageMetadata() : this(new Vec2I(0, 0), ResourceNamespace.Global)
        {
        }

        public ImageMetadata(Vec2I offset, ResourceNamespace resourceNamespace)
        {
            Offset = offset;
            Namespace = resourceNamespace;
        }

        public ImageMetadata(ResourceNamespace resourceNamespace)
        {
            Offset = new Vec2I(0, 0);
            Namespace = resourceNamespace;
        }
    }
}
