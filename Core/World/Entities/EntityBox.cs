using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Models;
using Helion.Util.Extensions;

namespace Helion.World.Entities;

public class EntityBox : BoundingBox3D
{
    private Vec3D m_centerBottom;
    public double Radius { get; }
    public double Height { get; private set; }

    public double Top => Max.Z;
    public double Bottom => Min.Z;
    public Vec3D Position => m_centerBottom;
    public Box2D To2D() => new(Min.XY, Max.XY);

    public EntityBox(Vec3D centerBottom, double radius, double height) :
        base(CalculateMin(centerBottom, radius), CalculateMax(centerBottom, radius, height))
    {
        m_centerBottom = centerBottom;
        Radius = radius;
        Height = height;
    }

    private static Vec3D CalculateMin(Vec3D centerBottom, double radius)
    {
        return new(centerBottom.X - radius, centerBottom.Y - radius, centerBottom.Z);
    }

    private static Vec3D CalculateMax(Vec3D centerBottom, double radius, double height)
    {
        return new(centerBottom.X + radius, centerBottom.Y + radius, centerBottom.Z + height);
    }

    public void MoveTo(Vec3D centerBottomPosition)
    {
        Vec3D delta = centerBottomPosition - m_centerBottom;

        m_centerBottom = centerBottomPosition;
        m_Min += delta;
        m_Max += delta;
    }

    public void SetZ(double bottomZ)
    {
        m_centerBottom.Z = bottomZ;
        m_Min.Z = bottomZ;
        m_Max.Z = bottomZ + Height;
    }

    public void SetXY(Vec2D position)
    {
        m_centerBottom.X = position.X;
        m_centerBottom.Y = position.Y;

        m_Min.X = position.X - Radius;
        m_Max.X = position.X + Radius;
        m_Min.Y = position.Y - Radius;
        m_Max.Y = position.Y + Radius;
    }

    public void SetHeight(double height)
    {
        Height = height;
        m_Max.Z = Min.Z + height;
    }

    public bool OverlapsZ(Box3D box) => Top > box.Min.Z && Bottom < box.Max.Z;
    public bool OverlapsZ(BoundingBox3D box) => Top > box.Min.Z && Bottom < box.Max.Z;

    public bool Intersects(Vec2D p1, Vec2D p2, ref Vec2D intersect)
    {
        if (p2.X < Min.X && p1.X < Min.X)
            return false;
        if (p2.X > Max.X && p1.X > Max.X)
            return false;
        if (p2.Y < Min.Y && p1.Y < Min.Y)
            return false;
        if (p2.Y > Max.Y && p1.Y > Max.Y)
            return false;
        if (p1.X > Min.X && p1.X < Max.X &&
            p1.Y > Min.Y && p1.Y < Max.Y)
        {
            intersect = p1;
            return true;
        }

        if ((p1.X < Min.X && Intersects(p1.X - Min.X, p2.X - Min.X, p1, p2, ref intersect) && intersect.Y > Min.Y && intersect.Y < Max.Y)
              || (p1.Y < Min.Y && Intersects(p1.Y - Min.Y, p2.Y - Min.Y, p1, p2, ref intersect) && intersect.X > Min.X && intersect.X < Max.X)
              || (p1.X > Max.X && Intersects(p1.X - Max.X, p2.X - Max.X, p1, p2, ref intersect) && intersect.Y > Min.Y && intersect.Y < Max.Y)
              || (p1.Y > Max.Y && Intersects(p1.Y - Max.Y, p2.Y - Max.Y, p1, p2, ref intersect) && intersect.X > Min.X && intersect.X < Max.X))
            return true;

        return false;
    }

    public bool Intersects(Vec3D p1, Vec3D p2, ref Vec3D intersect)
    {
        if (p2.X < Min.X && p1.X < Min.X)
            return false;
        if (p2.X > Max.X && p1.X > Max.X)
            return false;
        if (p2.Y < Min.Y && p1.Y < Min.Y)
            return false;
        if (p2.Y > Max.Y && p1.Y > Max.Y)
            return false;
        if (p2.Z < Min.Z && p1.Z < Min.Z)
            return false;
        if (p2.Z > Max.Z && p1.Z > Max.Z)
            return false;
        if (p1.X > Min.X && p1.X < Max.X &&
            p1.Y > Min.Y && p1.Y < Max.Y &&
            p1.Z > Min.Z && p1.Z < Max.Z)
        {
            intersect = p1;
            return true;
        }

        if ((p1.X < Min.X && Intersects(p1.X - Min.X, p2.X - Min.X, p1, p2, ref intersect) && intersect.Y > Min.Y && intersect.Y < Max.Y && intersect.Z > Min.Z && intersect.Z < Max.Z)
              || (p1.Y < Min.Y && Intersects(p1.Y - Min.Y, p2.Y - Min.Y, p1, p2, ref intersect) && intersect.X > Min.X && intersect.X < Max.X && intersect.Z > Min.Z && intersect.Z < Max.Z)
              || (p1.Z < Min.Z && Intersects(p1.Z - Min.Z, p2.Z - Min.Z, p1, p2, ref intersect) && intersect.X > Min.X && intersect.X < Max.X && intersect.Y > Min.Y && intersect.Y < Max.Y)
              || (p1.X > Max.X && Intersects(p1.X - Max.X, p2.X - Max.X, p1, p2, ref intersect) && intersect.Y > Min.Y && intersect.Y < Max.Y && intersect.Z > Min.Z && intersect.Z < Max.Z)
              || (p1.Y > Max.Y && Intersects(p1.Y - Max.Y, p2.Y - Max.Y, p1, p2, ref intersect) && intersect.X > Min.X && intersect.X < Max.X && intersect.Z > Min.Z && intersect.Z < Max.Z)
              || (p1.Z > Max.Z && Intersects(p1.Z - Max.Z, p2.Z - Max.Z, p1, p2, ref intersect) && intersect.X > Min.X && intersect.X < Max.X && intersect.Y > Min.Y && intersect.Y < Max.Y))
            return true;

        return false;
    }

    private static bool Intersects(double dist1, double dist2, Vec2D p1, Vec2D p2, ref Vec2D intersect)
    {
        if (dist1 * dist2 >= 0.0 || dist1.ApproxEquals(dist2))
            return false;

        intersect = p1 + ((p2 - p1) * (-dist1 / (dist2 - dist1)));
        return true;
    }

    private static bool Intersects(double dist1, double dist2, Vec3D p1, Vec3D p2, ref Vec3D intersect)
    {
        if (dist1 * dist2 >= 0.0 || dist1.ApproxEquals(dist2))
            return false;

        intersect = p1 + ((p2 - p1) * (-dist1 / (dist2 - dist1)));
        return true;
    }

    public EntityBoxModel ToEntityBoxModel()
    {
        return new()
        {
            CenterX = m_centerBottom.X,
            CenterY = m_centerBottom.Y,
            CenterZ = m_centerBottom.Z,
            Radius = Radius,
            Height = Height
        };
    }

    public override string ToString() => $"{base.ToString()} (center: {m_centerBottom}, radius: {Radius}, height: {Height})";
}

