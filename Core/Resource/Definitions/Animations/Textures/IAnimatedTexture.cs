using Helion.Util;

namespace Helion.Resource.Definitions.Animations.Textures
{
    /// <summary>
    /// A texture that is animatable.
    /// </summary>
    public interface IAnimatedTexture
    {
        /// <summary>
        /// The name of the texture.
        /// </summary>
        CIString Name { get; }

        /// <summary>
        /// The namespace of the texture.
        /// </summary>
        Namespace Namespace { get; }
    }
}
