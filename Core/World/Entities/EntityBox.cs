using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Models;
using Helion.Util.Extensions;

namespace Helion.World.Entities;

public struct EntityBox
{
    private Vec3D m_centerBottom;
    public double Radius;
    public double Height;
    public Vec3D Min;
    public Vec3D Max;

    public double Top => Max.Z;
    public double Bottom => Min.Z;
    public Vec3D Position => m_centerBottom;
    public Box2D To2D() => new(Min.XY, Max.XY);

    public EntityBox(Vec3D centerBottom, double radius, double height)
    {
        m_centerBottom = centerBottom;
        Radius = radius;
        Height = height;
        Min = CalculateMin(centerBottom, radius);
        Max = CalculateMax(centerBottom, radius, height);
    }

    public void Set(Vec3D centerBottom, double radius, double height)
    {
        Radius = radius;
        Height = height;
        m_centerBottom = centerBottom;
        Min = CalculateMin(centerBottom, radius);
        Max = CalculateMax(centerBottom, radius, height);
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
        Min += delta;
        Max += delta;
    }

    public void SetZ(double bottomZ)
    {
        m_centerBottom.Z = bottomZ;
        Min.Z = bottomZ;
        Max.Z = bottomZ + Height;
    }

    public void SetXY(Vec2D position)
    {
        m_centerBottom.X = position.X;
        m_centerBottom.Y = position.Y;

        Min.X = position.X - Radius;
        Max.X = position.X + Radius;
        Min.Y = position.Y - Radius;
        Max.Y = position.Y + Radius;
    }

    public void SetHeight(double height)
    {
        Height = height;
        Max.Z = Min.Z + height;
    }

    public bool Overlaps(in EntityBox box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
    public bool Overlaps2D(in EntityBox other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
    public bool Overlaps2D(in Box2D other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
    public bool OverlapsZ(in EntityBox box) => Top > box.Min.Z && Bottom < box.Max.Z;
    public bool OverlapsZ(in Box3D box) => Top > box.Min.Z && Bottom < box.Max.Z;

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

    public override bool Equals(object? obj)
    {
        if (obj is not EntityBox entityBox)
            return false;

        return entityBox.m_centerBottom == m_centerBottom &&
            entityBox.Min == Min &&
            entityBox.Max == Max &&
            entityBox.Radius == Radius &&
            entityBox.Height == Height;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
