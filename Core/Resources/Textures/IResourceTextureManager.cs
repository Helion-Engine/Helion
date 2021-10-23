using Helion.Resources.Textures.Animations;
using Helion.Resources.Textures.Sprites;

namespace Helion.Resources.Textures;

/// <summary>
/// Responsible for managing textures that come from a resource collection.
/// </summary>
public interface IResourceTextureManager
{
    /// <summary>
    /// The animations that the texture manager uses.
    /// </summary>
    IResourceTextureAnimationManager AnimationManager { get; }
    
    /// <summary>
    /// Manages sprites from this texture manager.
    /// </summary>
    IResourceSpriteManager ResourceSpriteManager { get; }
    
    /// <summary>
    /// Looks up the texture, or creates one if it has not been found yet.
    /// </summary>
    /// <param name="name">The texture name.</param>
    /// <param name="priorityNamespace">The first namespace to look in.</param>
    /// <param name="texture">Always returns a texture. If the function returns
    /// true, then a texture was created. It may not be from the same namespace
    /// though. If it returns false, that means it could not find, nor make the
    /// texture, and the "null texture" singleton was returned.
    /// </param>
    /// <returns>True if found (or made), false if unable to find.</returns>
    bool TryGet(string name, ResourceNamespace priorityNamespace, out ResourceTexture texture);
    
    /// <summary>
    /// Gets a texture by index. If the index is out of range, then the
    /// "null texture" singleton is returned.
    /// </summary>
    /// <param name="index">The index to get.</param>
    /// <returns>The texture, or the "null texture" singleton.</returns>
    ResourceTexture GetByIndex(int index);
    
    /// <summary>
    /// Get a texture by index, checks if the index is null and returns Doom's
    /// zero indexed texture (usually AASHITTY). This function is only intended
    /// to be used for vanilla compatibility like FloorRaiseByTexture.
    /// </summary>
    /// <param name="index">The index of the texture.</param>
    /// <returns>Returns a texture at the given index. If the texture is
    /// animated it's current animation texture will be returned.</returns>
    ResourceTexture GetNullCompatibilityTexture(int index);
}