using Helion.Graphics;
using System.Collections.Generic;

namespace Helion.Resources.Images;

/// <summary>
/// Responsible for retrieving images from some source.
/// </summary>
public interface IImageRetriever
{
    /// <summary>
    /// Gets the image. Priority is given to some namespace, but on failure
    /// it will look up the image from all other namespaces afterwards.
    /// </summary>
    /// <param name="name">The image name.</param>
    /// <param name="priorityNamespace">The namespace to check first.
    /// <param name="options">Options for fetching the image.
    /// </param>
    /// <returns>The image, or null if none can be found.</returns>
    Image? Get(string name, ResourceNamespace priorityNamespace, GetImageOptions options = GetImageOptions.Default);

    /// <summary>
    /// Gets the image in only the namespace provided. It will not look in
    /// any other namespace.
    /// </summary>
    /// <param name="name">The image name.</param>
    /// <param name="targetNamespace">The namespace to check.</param>
    /// <param name="options">Options for fetching the image.
    /// <returns>The image, or null if none can be found.</returns>
    Image? GetOnly(string name, ResourceNamespace targetNamespace, GetImageOptions options = GetImageOptions.Default);

    /// <summary>
    /// Gets the image in only the namespace provided. It will not look in
    /// any other namespace.
    /// </summary>
    /// <param name="mappedName">The name to map this image.</param>
    /// <param name="entryName">The name to search if the image is not mapped to mappedName.</param>
    /// <param name="targetNamespace">The namespace to check.</param>
    /// <param name="colorTranslation">Color translation table to generate the image.</param>
    /// <param name="options">Options for fetching the image.
    /// <returns>The image, or null if none can be found.</returns>
    Image? GetOnlyMapped(string mappedName, string entryName, ResourceNamespace targetNamespace, byte[]? colorTranslation, GetImageOptions options = GetImageOptions.Default);

    /// <summary>
    /// Get the names of all images in the specific namespace
    /// </summary>
    /// <param name="specificNamespace">The desired namespace</param>
    /// <returns>A list of image names</returns>
    IEnumerable<string> GetNames(ResourceNamespace specificNamespace);
}
