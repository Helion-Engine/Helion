using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Resources;

namespace Helion.Render.Common.Textures;

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
    bool HasImage(string name, ResourceNamespace? specificNamespace = null)
    {
        return TryGet(name, out _, specificNamespace);
    }

    /// <summary>
    /// Tries to get a handle for an image.
    /// </summary>
    /// <param name="name">The image name.</param>
    /// <param name="handle">The handle, which is not null if it returns true,
    /// or null if it returns false.</param>
    /// <param name="specificNamespace">If null, will search all namespaces,
    /// otherwise will search only in the provided one.</param>
    /// <returns>True if found, false if not.</returns>
    bool TryGet(string name, [NotNullWhen(true)] out IRenderableTextureHandle? handle, ResourceNamespace? specificNamespace = null);

    /// <summary>
    /// Get a list of texture names
    /// </summary>
    /// <param name="specificNamespace">Namespace to filter to</param>
    /// <returns>A list of texture names</returns>
    IEnumerable<string> GetNames(ResourceNamespace specificNamespace);
}
