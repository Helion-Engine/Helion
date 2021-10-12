using System;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Spheres;
using Helion.Geometry.Vectors;

namespace Helion.Geometry.Rays;

public readonly struct Ray3D
{
    public readonly Vec3D Origin;
    public readonly Vec3D Direction;

    public Ray3D(Vec3D direction) : this(Vec3D.Zero, direction)
    {
    }

    public Ray3D(Vec3D origin, Vec3D direction)
    {
        Origin = origin;
        Direction = direction.Unit();
    }

    public static Ray3D operator +(Ray3D self, Vec3D vec) => new(self.Origin + vec, self.Direction);
    public static Ray3D operator +(Ray3D self, Vector3D vec) => new(self.Origin + vec, self.Direction);
    public static Ray3D operator -(Ray3D self, Vec3D vec) => new(self.Origin - vec, self.Direction);
    public static Ray3D operator -(Ray3D self, Vector3D vec) => new(self.Origin - vec, self.Direction);
    public static bool operator ==(Ray3D self, Ray3D other) => self.Origin == other.Origin && self.Direction == other.Direction;
    public static bool operator !=(Ray3D self, Ray3D other) => !(self == other);

    public override string ToString() => $"({Origin}), ({Direction})";
    public override bool Equals(object? obj) => obj is Ray3D r && Origin == r.Origin && Direction == r.Direction;
    public override int GetHashCode() => HashCode.Combine(Origin.GetHashCode(), Direction.GetHashCode());
}
