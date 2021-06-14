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
        Dimension Dimension { get; }
        ImageType ImageType { get; }
        Vec2I Offset { get; }
        ResourceNamespace Namespace { get; }

        int Width => Dimension.Width;
        int Height => Dimension.Height;
    }
}
