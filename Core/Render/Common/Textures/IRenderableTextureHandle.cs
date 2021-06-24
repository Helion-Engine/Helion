using Helion.Geometry;
using Helion.Geometry.Boxes;

namespace Helion.Render.Common.Textures
{
    /// <summary>
    /// A texture handle that can be rendered with.
    /// </summary>
    public interface IRenderableTextureHandle
    {
        /// <summary>
        /// A specific lookup index into a renderer table. Used for quick access
        /// of the texture information. This number is specific to whatever the
        /// rendering implementation uses.
        /// </summary>
        int Index { get; }
        
        /// <summary>
        /// The box around the pixels on the supporting image. For atlases, this
        /// will be the location of the pixels relative to the origin. For any
        /// standalone images, it will be the entire thing.
        /// </summary>
        Box2I Area { get; }
        
        /// <summary>
        /// The UV coordinates of the image. This is based off of the
        /// <see cref="Area"/>'s location in the image. The numbers will be
        /// specific the renderer that implements it.
        /// </summary>
        /// <remarks>
        /// This means that for Vulkan, it will use the top left corner as (0, 0).
        /// However for OpenGL, that means the bottom left corner will be (0, 0).
        /// </remarks>
        Box2F UV { get; }

        /// <summary>
        /// The dimension of the image. This is a shortcut for the area.
        /// </summary>
        Dimension Dimension { get; }
    }
}
