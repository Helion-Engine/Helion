using System.Runtime.InteropServices;

namespace Helion.Geometry.New;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Plane
{
    public readonly float A;
    public readonly float B;
    public readonly float C;
    private float m_d;
    private float m_inverseC;

    public float D => m_d;
    public Vec3 Normal => (A, B, C);

    private Plane(Vec3 normalUnit, float d, float inverseC)
    {
        A = normalUnit.X;
        B = normalUnit.Y;
        C = normalUnit.Z;
        m_d = d;
        m_inverseC = inverseC;
    }

    public Plane(float a, float b, float c, float d) : this(new Vec3(a, b, c).Unit, d, 1.0f / c)
    {
    }

    public void MoveZ(float amount) => m_d -= amount * C;
    public float Z(Vec2 point) => -(m_d + (A * point.X) + (B * point.Y)) * m_inverseC;
    public float Z(Vec3 point) => -(m_d + (A * point.X) + (B * point.Y)) * m_inverseC;
}
