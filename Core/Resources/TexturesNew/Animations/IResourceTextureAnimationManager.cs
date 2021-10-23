using Helion.Util;

namespace Helion.Resources.TexturesNew.Animations;

/// <summary>
/// Manages animations for a texture manager.
/// </summary>
public interface IResourceTextureAnimationManager : ITickable
{
    /// <summary>
    /// Gets the texture that should be used for the provided texture.
    /// </summary>
    /// <remarks>
    /// For example, if we pass in NUKAGE1, this might return NUKAGE1 again
    /// if the animations point to it. Likewise, maybe passing in NUKAGE1
    /// could return NUKAGE3.
    /// </remarks>
    /// <param name="texture">The texture to use to lookup the correct
    /// animation.</param>
    /// <returns>Either the same texture, or the animated texture for this
    /// texture.</returns>
    ResourceTexture Lookup(ResourceTexture texture) => Lookup(texture.Index);
    
    /// <summary>
    /// Looks up the texture based on its texture index.
    /// </summary>
    /// <param name="textureIndex">The texture index.</param>
    /// <returns>The texture that maps onto this. Will be the "null texture"
    /// singleton if the index is out of range.</returns>
    ResourceTexture Lookup(int textureIndex);
}