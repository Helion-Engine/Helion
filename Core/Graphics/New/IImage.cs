using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Resources;

namespace Helion.Graphics.New
{
    /// <summary>
    /// An image backed by some raster.
    /// </summary>
    public interface IImage
    {
        Bitmap Bitmap { get; }
        Dimension Dimension { get; }
        int Width { get; }
        int Height { get; }
        ImageType ImageType { get; }
        Vec2I Offset { get; }
        ResourceNamespace Namespace { get; }
    }
}
