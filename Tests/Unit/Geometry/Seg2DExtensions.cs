using Helion.Geometry.Segments;

namespace Helion.Tests.Unit.Geometry;

internal static class Seg2DExtensions
{
    public static bool IntersectionInclusive(this Seg2D thisSeg, Seg2D seg, out double t) => 
        thisSeg.IntersectionInclusive(seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y, out t);
}
