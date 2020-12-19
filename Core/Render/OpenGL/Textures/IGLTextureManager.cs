using System;
using Helion.Render.Shared;

namespace Helion.Render.OpenGL.Textures
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