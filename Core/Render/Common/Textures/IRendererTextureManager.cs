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
    /// <param name="upscaleFactor">Upscale factor to apply if retrieving the image for the first time.</param>
    /// <returns>True if found, false if not.</returns>
    bool TryGet(string name, [NotNullWhen(true)] out IRenderableTextureHandle? handle, ResourceNamespace? specificNamespace = null, int upscaleFactor = 1);

    /// <summary>
    /// Get a list of texture names
    /// </summary>
    /// <param name="specificNamespace">Namespace to filter to</param>
    /// <returns>A list of texture names</returns>
    IEnumerable<string> GetNames(ResourceNamespace specificNamespace);

    /// <summary>
    /// Stores a new image in the texture manager.  It is the consumer's responsibility to remove and dispose this texture when it is no longer needed.
    /// </summary>
    /// <param name="name">Name to assign the texture</param>
    /// <param name="resourceNamespace">Resource namespace to store the texture in</param>
    /// <param name="image">Image data</param>
    /// <param name="repeatY">Whether the image should repeat vertically</param>
    /// <param name="removeAction">An action, that when invoked, will remove the texture from tracking.</param>
    /// <returns>A handle to the created texture</returns>
    IRenderableTextureHandle CreateAndTrackTexture(string name, ResourceNamespace resourceNamespace, Graphics.Image image, out Action removeAction, bool repeatY = true);

    /// <summary>
    /// Remove a texture from the texture manager
    /// </summary>
    /// <param name="name">Name of the texture</param>
    /// <param name="resourceNamespace">Namespace for the texture</param>
    void RemoveTexture(string name, ResourceNamespace resourceNamespace);
}
