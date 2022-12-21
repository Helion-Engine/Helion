using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Models;
using Helion.Util.Extensions;
using static Microsoft.FSharp.Core.ByRefKinds;

namespace Helion.World.Entities;

public partial class Entity
{
    public Box2D GetBox2D() => new(BoxMin.XY, BoxMax.XY);

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
        Vec3D delta = centerBottomPosition - Position;

        Position = centerBottomPosition;
        BoxMin += delta;
        BoxMax += delta;
    }

    public void SetZ(double bottomZ)
    {
        Position.Z = bottomZ;
        BoxMin.Z = bottomZ;
        BoxMax.Z = bottomZ + Height;
    }

    public bool Overlaps(in Box3D box) => !(BoxMin.X >= box.Max.X || BoxMax.X <= box.Min.X || BoxMin.Y >= box.Max.Y || BoxMax.Y <= box.Min.Y || BoxMin.Z >= box.Max.Z || BoxMax.Z <= box.Min.Z);
    public bool Overlaps2D(Entity other) => !(BoxMin.X >= other.BoxMax.X || BoxMax.X <= other.BoxMin.X || BoxMin.Y >= other.BoxMax.Y || BoxMax.Y <= other.BoxMin.Y);
    public bool Overlaps2D(in Box2D other) => !(BoxMin.X >= other.Max.X || BoxMax.X <= other.Min.X || BoxMin.Y >= other.Max.Y || BoxMax.Y <= other.Min.Y);
    public bool OverlapsZ(Entity other) => BoxMax.Z > other.BoxMin.Z && BoxMin.Z < other.BoxMax.Z;
    public bool OverlapsZ(in Box3D box) => BoxMax.Z > box.Min.Z && BoxMin.Z < box.Max.Z;

    public bool BoxIntersects(Vec2D p1, Vec2D p2, ref Vec2D intersect)
    {
        if (p2.X < BoxMin.X && p1.X < BoxMin.X)
            return false;
        if (p2.X > BoxMax.X && p1.X > BoxMax.X)
            return false;
        if (p2.Y < BoxMin.Y && p1.Y < BoxMin.Y)
            return false;
        if (p2.Y > BoxMax.Y && p1.Y > BoxMax.Y)
            return false;
        if (p1.X > BoxMin.X && p1.X < BoxMax.X &&
            p1.Y > BoxMin.Y && p1.Y < BoxMax.Y)
        {
            intersect = p1;
            return true;
        }

        if ((p1.X < BoxMin.X && Intersects(p1.X - BoxMin.X, p2.X - BoxMin.X, p1, p2, ref intersect) && intersect.Y > BoxMin.Y && intersect.Y < BoxMax.Y)
              || (p1.Y < BoxMin.Y && Intersects(p1.Y - BoxMin.Y, p2.Y - BoxMin.Y, p1, p2, ref intersect) && intersect.X > BoxMin.X && intersect.X < BoxMax.X)
              || (p1.X > BoxMax.X && Intersects(p1.X - BoxMax.X, p2.X - BoxMax.X, p1, p2, ref intersect) && intersect.Y > BoxMin.Y && intersect.Y < BoxMax.Y)
              || (p1.Y > BoxMax.Y && Intersects(p1.Y - BoxMax.Y, p2.Y - BoxMax.Y, p1, p2, ref intersect) && intersect.X > BoxMin.X && intersect.X < BoxMax.X))
            return true;

        return false;
    }

    public bool BoxIntersects(Vec3D p1, Vec3D p2, ref Vec3D intersect)
    {
        if (p2.X < BoxMin.X && p1.X < BoxMin.X)
            return false;
        if (p2.X > BoxMax.X && p1.X > BoxMax.X)
            return false;
        if (p2.Y < BoxMin.Y && p1.Y < BoxMin.Y)
            return false;
        if (p2.Y > BoxMax.Y && p1.Y > BoxMax.Y)
            return false;
        if (p2.Z < BoxMin.Z && p1.Z < BoxMin.Z)
            return false;
        if (p2.Z > BoxMax.Z && p1.Z > BoxMax.Z)
            return false;
        if (p1.X > BoxMin.X && p1.X < BoxMax.X &&
            p1.Y > BoxMin.Y && p1.Y < BoxMax.Y &&
            p1.Z > BoxMin.Z && p1.Z < BoxMax.Z)
        {
            intersect = p1;
            return true;
        }

        if ((p1.X < BoxMin.X && Intersects(p1.X - BoxMin.X, p2.X - BoxMin.X, p1, p2, ref intersect) && intersect.Y > BoxMin.Y && intersect.Y < BoxMax.Y && intersect.Z > BoxMin.Z && intersect.Z < BoxMax.Z)
              || (p1.Y < BoxMin.Y && Intersects(p1.Y - BoxMin.Y, p2.Y - BoxMin.Y, p1, p2, ref intersect) && intersect.X > BoxMin.X && intersect.X < BoxMax.X && intersect.Z > BoxMin.Z && intersect.Z < BoxMax.Z)
              || (p1.Z < BoxMin.Z && Intersects(p1.Z - BoxMin.Z, p2.Z - BoxMin.Z, p1, p2, ref intersect) && intersect.X > BoxMin.X && intersect.X < BoxMax.X && intersect.Y > BoxMin.Y && intersect.Y < BoxMax.Y)
              || (p1.X > BoxMax.X && Intersects(p1.X - BoxMax.X, p2.X - BoxMax.X, p1, p2, ref intersect) && intersect.Y > BoxMin.Y && intersect.Y < BoxMax.Y && intersect.Z > BoxMin.Z && intersect.Z < BoxMax.Z)
              || (p1.Y > BoxMax.Y && Intersects(p1.Y - BoxMax.Y, p2.Y - BoxMax.Y, p1, p2, ref intersect) && intersect.X > BoxMin.X && intersect.X < BoxMax.X && intersect.Z > BoxMin.Z && intersect.Z < BoxMax.Z)
              || (p1.Z > BoxMax.Z && Intersects(p1.Z - BoxMax.Z, p2.Z - BoxMax.Z, p1, p2, ref intersect) && intersect.X > BoxMin.X && intersect.X < BoxMax.X && intersect.Y > BoxMin.Y && intersect.Y < BoxMax.Y))
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
            CenterX = Position.X,
            CenterY = Position.Y,
            CenterZ = Position.Z,
            Radius = Radius,
            Height = Height
        };
    }
}
