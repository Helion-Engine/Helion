using System;
using Helion.Geometry;
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

        /// <summary>
        /// Gets the dimension of an image.
        /// </summary>
        /// <param name="name">The image name.</param>
        /// <param name="dimension">The resultant dimension.</param>
        /// <param name="specificNamespace">If null, will search all namespaces,
        /// otherwise will search only in the provided one.</param>
        /// <returns>True if it was found (and the dimension value will be set
        /// correctly), or false, and the dimension will be set to 1x1.</returns>
        bool TryGetImageDimension(string name, out Dimension dimension, ResourceNamespace? specificNamespace = null);
    }
}
