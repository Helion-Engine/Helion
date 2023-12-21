using Helion.Graphics;

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
    /// </param>
    /// <returns>The image, or null if none can be found.</returns>
    Image? Get(string name, ResourceNamespace priorityNamespace);

    /// <summary>
    /// Gets the image in only the namespace provided. It will not look in
    /// any other namespace.
    /// </summary>
    /// <param name="name">The image name.</param>
    /// <param name="targetNamespace">The namespace to check.</param>
    /// <returns>The image, or null if none can be found.</returns>
    Image? GetOnly(string name, ResourceNamespace targetNamespace);
}
