using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Resources;

namespace Helion.Render.Legacy.Shared;

/// <summary>
/// A helper class used with calculating the area that a string would take
/// up on the screen.
/// </summary>
public interface IImageDrawInfoProvider
{
    /// <summary>
    /// Checks if the image exists currently in the texture manager.
    /// </summary>
    /// <param name="image">The image name.</param>
    /// <returns>True if the image exists, false if not.</returns>
    bool ImageExists(string image);

    /// <summary>
    /// Gets the dimension of the image.
    /// </summary>
    /// <param name="image">The name of the image.</param>
    /// <param name="resourceNamespace">The namespace of the resource. This
    /// is optional.</param>
    /// <returns>The dimension of the image.</returns>
    Dimension GetImageDimension(string image, ResourceNamespace resourceNamespace = ResourceNamespace.Global);
    Vec2I GetImageOffset(string image, ResourceNamespace resourceNamespace = ResourceNamespace.Global);

    /// <summary>
    /// Gets the vertical height of the font, which is the tallest
    /// character that can be rendered from the bottom.
    /// </summary>
    /// <param name="font">The name of the font.</param>
    /// <returns>The font height in pixels.</returns>
    int GetFontHeight(string font);
}
