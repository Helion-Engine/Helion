using Helion.Geometry.Vectors;
using Helion.Util.Extensions;

namespace Helion.Tests.Unit.GameAction;

public static class VectorExtensions
{
    public static bool ApproxEquals(this Vec3D v1, Vec3D v2)
    {
        return v1.X.ApproxEquals(v2.X) && v1.Y.ApproxEquals(v2.Y) && v1.Z.ApproxEquals(v2.Z);
    }
}
