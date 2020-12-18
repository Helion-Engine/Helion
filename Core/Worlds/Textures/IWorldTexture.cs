using Helion.Resource.Textures;
using Helion.Util;

namespace Helion.Worlds.Textures
{
    /// <summary>
    /// A texture that is applied to some line or flat in a world.
    /// </summary>
    /// <remarks>
    /// Intended to be an abstraction over a texture so that textures in the
    /// world can be managed without having to interfere with the resources
    /// texture manager.
    /// </remarks>
    public interface IWorldTexture
    {
        /// <summary>
        /// The name of this texture.
        /// </summary>
        CIString Name { get; }

        /// <summary>
        /// The current texture that should be rendered with.
        /// </summary>
        /// <remarks>
        /// If this is an animated implementation, this should get the texture
        /// that should be rendered with upon inspection.
        /// </remarks>
        Texture Texture { get; }

        /// <summary>
        /// If this is the null texture.
        /// </summary>
        bool IsMissing { get; }

        /// <summary>
        /// If this is a sky texture.
        /// </summary>
        bool IsSky { get; }
    }
}
