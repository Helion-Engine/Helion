using Helion.GeometryNew.Vectors;
using Helion.Util;

namespace Helion.GeometryNew.Planes;

public struct Plane
{
    public float A;
    public float B;
    public float C;
    public float D;

    public Plane(float a, float b, float c, float d)
    {
        A = a;
        B = b;
        C = c;
        D = d;
    }

    public bool Intersects(Vec3 p, Vec3 q, ref Vec3 intersect)
    {
        Vec3 normal = (A, B, C);
        Vec3 delta = q - p;

        float denominator = normal.Dot(delta);
        if (MathHelper.IsZero(denominator))
            return false;

        float t = -(normal.Dot(p) + D) / denominator;
        intersect = p + (t * delta);
        return true;
    }
}
