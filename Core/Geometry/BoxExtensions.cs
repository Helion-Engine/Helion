using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helion.Geometry;

public static class BoxExtensions
{
    // Checks 180 FOV. Not true frustum culling.
    public static bool InView(this Box2D box, Vec2D viewPos, Vec2D viewDirection)
    {
        Vec2D p1 = new(box.Min.X - viewPos.X, box.Min.Y - viewPos.Y);
        Vec2D p2 = new(box.Max.X - viewPos.X, box.Max.Y - viewPos.Y);
        Vec2D p3 = new(box.Min.X - viewPos.X, box.Max.Y - viewPos.Y);
        Vec2D p4 = new(box.Max.X - viewPos.X, box.Min.Y - viewPos.Y);
        return p1.Dot(viewDirection) >= 0 || p2.Dot(viewDirection) >= 0 || p3.Dot(viewDirection) >= 0 || p4.Dot(viewDirection) >= 0;
    }

    public static bool InView(this Seg2D seg, Vec2D viewPos, Vec2D viewDirection)
    {
        Vec2D p1 = new(seg.Start.X - viewPos.X, seg.Start.Y - viewPos.Y);
        Vec2D p2 = new (seg.End.X - viewPos.X, seg.End.Y - viewPos.Y);
        return p1.Dot(viewDirection) >= 0 || p2.Dot(viewDirection) >= 0;
    }
}
