using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Shared;

namespace Helion.Render.OpenGL.Texture;

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
