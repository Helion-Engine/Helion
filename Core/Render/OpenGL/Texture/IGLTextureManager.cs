using System;
using Helion.Render.Shared;
using Helion.Resources;
using Helion.Util;

namespace Helion.Render.OpenGL.Texture
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