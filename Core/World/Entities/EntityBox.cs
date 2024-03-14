using System;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Models;
using Helion.Util.Extensions;

namespace Helion.World.Entities;

public partial class Entity
{
    public Box2D GetBox2D() => new((Position.X - Radius, Position.Y - Radius), (Position.X + Radius, Position.Y + Radius));

    public bool BoxIntersects(Vec2D p1, Vec2D p2, ref Vec2D intersect)
    {
        Vec2D min = new(Position.X - Radius, Position.Y - Radius);
        Vec2D max = new(Position.X + Radius, Position.Y + Radius);

        if (p2.X < min.X && p1.X < min.X)
            return false;
        if (p2.X > max.X && p1.X > max.X)
            return false;
        if (p2.Y < min.Y && p1.Y < min.Y)
            return false;
        if (p2.Y > max.Y && p1.Y > max.Y)
            return false;
        if (p1.X > min.X && p1.X < max.X &&
            p1.Y > min.Y && p1.Y < max.Y)
        {
            intersect = p1;
            return true;
        }

        if ((p1.X < min.X && Intersects(p1.X - min.X, p2.X - min.X, p1, p2, ref intersect) && intersect.Y > min.Y && intersect.Y < max.Y)
              || (p1.Y < min.Y && Intersects(p1.Y - min.Y, p2.Y - min.Y, p1, p2, ref intersect) && intersect.X > min.X && intersect.X < max.X)
              || (p1.X > max.X && Intersects(p1.X - max.X, p2.X - max.X, p1, p2, ref intersect) && intersect.Y > min.Y && intersect.Y < max.Y)
              || (p1.Y > max.Y && Intersects(p1.Y - max.Y, p2.Y - max.Y, p1, p2, ref intersect) && intersect.X > min.X && intersect.X < max.X))
            return true;

        return false;
    }

    public bool BoxIntersects(Vec3D p1, Vec3D p2, ref Vec3D intersect)
    {
        Vec3D min = new(Position.X - Radius, Position.Y - Radius, Position.Z);
        Vec3D max = new(Position.X + Radius, Position.Y + Radius, Position.Z + Height);

        if (p2.X < min.X && p1.X < min.X)
            return false;
        if (p2.X > max.X && p1.X > max.X)
            return false;
        if (p2.Y < min.Y && p1.Y < min.Y)
            return false;
        if (p2.Y > max.Y && p1.Y > max.Y)
            return false;
        if (p2.Z < min.Z && p1.Z < min.Z)
            return false;
        if (p2.Z > max.Z && p1.Z > max.Z)
            return false;
        if (p1.X > min.X && p1.X < max.X &&
            p1.Y > min.Y && p1.Y < max.Y &&
            p1.Z > min.Z && p1.Z < max.Z)
        {
            intersect = p1;
            return true;
        }

        if ((p1.X < min.X && Intersects(p1.X - min.X, p2.X - min.X, p1, p2, ref intersect) && intersect.Y > min.Y && intersect.Y < max.Y && intersect.Z > min.Z && intersect.Z < max.Z)
              || (p1.Y < min.Y && Intersects(p1.Y - min.Y, p2.Y - min.Y, p1, p2, ref intersect) && intersect.X > min.X && intersect.X < max.X && intersect.Z > min.Z && intersect.Z < max.Z)
              || (p1.Z < min.Z && Intersects(p1.Z - min.Z, p2.Z - min.Z, p1, p2, ref intersect) && intersect.X > min.X && intersect.X < max.X && intersect.Y > min.Y && intersect.Y < max.Y)
              || (p1.X > max.X && Intersects(p1.X - max.X, p2.X - max.X, p1, p2, ref intersect) && intersect.Y > min.Y && intersect.Y < max.Y && intersect.Z > min.Z && intersect.Z < max.Z)
              || (p1.Y > max.Y && Intersects(p1.Y - max.Y, p2.Y - max.Y, p1, p2, ref intersect) && intersect.X > min.X && intersect.X < max.X && intersect.Z > min.Z && intersect.Z < max.Z)
              || (p1.Z > max.Z && Intersects(p1.Z - max.Z, p2.Z - max.Z, p1, p2, ref intersect) && intersect.X > min.X && intersect.X < max.X && intersect.Y > min.Y && intersect.Y < max.Y))
            return true;

        return false;
    }

    public bool Overlaps(in Box3D box) => 
        !(Position.X - Radius >= box.Max.X || 
        Position.X + Radius <= box.Min.X || 
        Position.Y - Radius >= box.Max.Y || 
        Position.Y + Radius <= box.Min.Y || 
        Position.Z >= box.Max.Z || 
        Position.Z + Height <= box.Min.Z);

    public bool Overlaps2D(Entity other) => 
        !(Position.X - Radius >= other.Position.X + other.Radius || 
        Position.X + Radius <= other.Position.X - other.Radius || 
        Position.Y - Radius >= other.Position.Y + other.Radius || 
        Position.Y + Radius <= other.Position.Y - Radius);

    public bool Overlaps2D(in Box2D other) => 
        !(Position.X - Radius >= other.Max.X || 
        Position.X + Radius <= other.Min.X || 
        Position.Y - Radius >= other.Max.Y || 
        Position.Y + Radius <= other.Min.Y);

    public bool OverlapsZ(Entity other) =>
        Position.Z + Height > other.Position.Z && 
        Position.Z < other.Position.Z + other.Height;

    public bool OverlapsZ(Entity other, double otherHeight) =>
        Position.Z + Height > other.Position.Z &&
        Position.Z < other.Position.Z + otherHeight;

    public bool OverlapsMissileClipZ(Entity other, bool missileClipCompat) =>
        OverlapsZ(other, other.GetMissileClipHeight(missileClipCompat));

    public double GetClampHeight()
    {
        if (Flags.SpawnCeiling)
            return GetMissileClipHeight(true);

        return Height;
    }

    public double GetMissileClipHeight(bool missileClipCompat)
    {
        int passHeight = Properties.ProjectilePassHeight;
        if (passHeight == 0)
            return Height;

        if (passHeight > 0)
            return passHeight;

        return missileClipCompat ? Math.Abs(passHeight) : Height;
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
            CenterX = Position.X,
            CenterY = Position.Y,
            CenterZ = Position.Z,
            Radius = Radius,
            Height = Height
        };
    }
}
