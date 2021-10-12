using System;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Geometry.Spheres;

public struct Sphere3D
{
    public Vec3D Origin;
    public double Radius;

    public Sphere3D(double radius) : this(Vec3D.Zero, radius)
    {
    }

    public Sphere3D(Vec3D origin, double radius)
    {
        Precondition(radius >= 0, "Sphere cannot have a negative radius");

        Origin = origin;
        Radius = radius;
    }

    public static Sphere3D operator +(Sphere3D self, Vec3D vec) => new(self.Origin + vec, self.Radius);
    public static Sphere3D operator +(Sphere3D self, Vector3D vec) => new(self.Origin + vec, self.Radius);
    public static Sphere3D operator -(Sphere3D self, Vec3D vec) => new(self.Origin - vec, self.Radius);
    public static Sphere3D operator -(Sphere3D self, Vector3D vec) => new(self.Origin - vec, self.Radius);
    public static bool operator ==(Sphere3D self, Sphere3D other) => self.Radius == other.Radius && self.Origin == other.Origin;
    public static bool operator !=(Sphere3D self, Sphere3D other) => !(self == other);

    public Vec3D ToPoint(double yaw, double pitch) => (Vec3D.UnitSphere(yaw, pitch) * Radius) + Origin;
    public bool Contains(Vec3D point) => Origin.Distance(point) <= Radius;
    public bool Contains(Vector3D point) => Origin.Distance(point) <= Radius;

    public bool Intersects(Sphere3D other)
    {
        double distanceSquared = Origin.DistanceSquared(other.Origin);
        double radSumSquared = (Radius + other.Radius) * (Radius + other.Radius);
        return distanceSquared < radSumSquared;
    }

    public bool Intersects(Seg3D seg)
    {
        // TODO
        return false;
    }

    public bool IntersectsAsLine(Seg3D seg)
    {
        // TODO
        return false;
    }

    public bool Intersects(Box3D box)
    {
        // TODO
        return false;
    }

    public override string ToString() => $"({Origin}), {Radius}";
    public override bool Equals(object? obj) => obj is Sphere3D c && Radius == c.Radius && Origin == c.Origin;
    public override int GetHashCode() => HashCode.Combine(Radius.GetHashCode(), Origin.GetHashCode());
}

