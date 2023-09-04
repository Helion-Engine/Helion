using Helion.GeometryNew.Vectors;

namespace Helion.GeometryNew.Planes;

public struct MoveablePlane
{
    private Plane m_plane;
    private readonly float m_inverseC;
    
    public float A => m_plane.A;
    public float B => m_plane.B;
    public float C => m_plane.C;
    public float D => m_plane.D;

    public MoveablePlane(float a, float b, float c, float d) : this(new(a, b, c, d))
    {
    }
    
    public MoveablePlane(Plane plane)
    {
        m_plane = plane;
        m_inverseC = 1.0f / plane.C;
    }
    
    public float ToZ(Vec2 point) => -(D + (A * point.X) + (B * point.Y)) * m_inverseC;
    public float ToZ(Vec3 point) => -(D + (A * point.X) + (B * point.Y)) * m_inverseC;
    
    public void MoveZ(float amount)
    {
        m_plane.D -= amount * C;
    }
    
    public bool Intersects(Vec3 p, Vec3 q, ref Vec3 intersect)
    {
        return m_plane.Intersects(p, q, ref intersect);
    }
}