using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics.String;
using Helion.Resources;

namespace Helion.Render.Legacy.Shared
{
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
        
        /// <summary>
        /// Calculates the rectangle draw area of the string for the font and
        /// font size (if any) provided.
        /// </summary>
        /// <param name="str">The string to calculate.</param>
        /// <param name="font">The font to draw with.</param>
        /// <param name="fontSize">The size of the font.</param>
        /// <param name="maxWidth">The width to either wrap or stop drawing at.
        /// By default is (effectively) infinity.</param>
        /// <param name="wrap">True if it would wrap around at the max width
        /// provided previously, false if it should not. If the text reaches
        /// the wrapping value then it will stop drawing if this is false.
        /// </param>
        /// <returns>The dimension the text will be drawn.</returns>
        Dimension GetDrawArea(ColoredString str, string font, int fontSize, int maxWidth = int.MaxValue, bool wrap = false);
     }
 }