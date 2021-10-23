using System.Diagnostics.CodeAnalysis;

namespace Helion.Resources.TexturesNew.Sprites;

/// <summary>
/// Manages sprites from a texture manager.
/// </summary>
public interface IResourceSpriteManager
{
    /// <summary>
    /// Tries to get a sprite with the five letter name provided (ex: PLAYA).
    /// </summary>
    /// <param name="name">The five letter sprite name.</param>
    /// <param name="sprite">The sprite if found, null if not.</param>
    /// <returns>True if found, false if not.</returns>
    bool TryGet(string name, [NotNullWhen(true)] out ResourceSpriteDefinition? sprite);
}