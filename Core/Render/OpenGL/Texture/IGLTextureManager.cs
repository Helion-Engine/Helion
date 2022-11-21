using Helion;
using Helion.Render;
using Helion.Render.Common.Shared;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Texture;

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
