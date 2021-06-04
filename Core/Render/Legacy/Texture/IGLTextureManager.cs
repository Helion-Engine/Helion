using System;
using Helion.Render.Legacy.Shared;

namespace Helion.Render.Legacy.Texture
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