using System;
using Helion.Render.OpenGL.Legacy.Shared;

namespace Helion.Render.OpenGL.Legacy.Texture
{
    /// <summary>
    /// Provides texture retrieval of loaded resources.
    /// </summary>
    public interface IGLTextureManager : IDisposable
    {
        /// <summary>
        /// The string render size area calculation.
        /// </summary>
        IImageDrawInfoProvider ImageDrawInfoProvider { get; }
    }
}