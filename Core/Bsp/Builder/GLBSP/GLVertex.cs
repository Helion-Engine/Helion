using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;

namespace Helion.Bsp.Builder.GLBSP
{
    public struct GLVertex
    {
        public const int Bytes = 8;
        
        public readonly Fixed X;
        public readonly Fixed Y;

        public GLVertex(Fixed x, Fixed y)
        {
            X = x;
            Y = y;
        }

        public Vec2D ToDouble() => new Vec2D(X.ToDouble(), Y.ToDouble());
    }
}