using System;
using Helion.Resources;

namespace Helion.Render.Common.Textures
{
    /// <summary>
    /// A texture manager for the renderer.
    /// </summary>
    public interface IRendererTextureManager : IDisposable
    {
        /// <summary>
        /// Checks if the image exists and is renderable with.
        /// </summary>
        /// <param name="name">The case insensitive name of the image.</param>
        /// <param name="specificNamespace">If null, will search all namespaces,
        /// otherwise will search only in the provided one.</param>
        /// <returns>True if such an image exists, false otherwise.</returns>
        bool HasImage(string name, ResourceNamespace? specificNamespace = null);
    }
}
