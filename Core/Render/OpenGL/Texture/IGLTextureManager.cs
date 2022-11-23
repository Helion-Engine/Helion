using Helion.Render.Common.Textures;
using Helion.Render.Legacy.Shared;

namespace Helion.Render.Legacy.Texture;

/// <summary>
/// Provides texture retrieval of loaded resources.
/// </summary>
public interface IGLTextureManager : IRendererTextureManager
{
    /// <summary>
    /// The string render size area calculation.
    /// </summary>
    IImageDrawInfoProvider ImageDrawInfoProvider { get; }
}
