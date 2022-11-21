using Helion.Geometry.Boxes;
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
        Vec2D p1 = box.Min - viewPos;
        Vec2D p2 = box.Max - viewPos;
        Vec2D p3 = (box.Min.X, box.Max.Y) - viewPos;
        Vec2D p4 = (box.Max.X, box.Min.Y) - viewPos;
        return p1.Dot(viewDirection) >= 0 || p2.Dot(viewDirection) >= 0 || p3.Dot(viewDirection) >= 0 || p4.Dot(viewDirection) >= 0;
    }
}
