using System;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Geometry.Spheres
{
    public struct Circle2D
    {
        public Vec2D Origin;
        public double Radius;

        public Box2D Box => new(Origin - new Vec2D(Radius, Radius), Origin + new Vec2D(Radius, Radius));

        public Circle2D(double radius) : this(Vec2D.Zero, radius)
        {
        }
        
        public Circle2D(Vec2D origin, double radius)
        {
            Precondition(radius >= 0, "Circle cannot have a negative radius");
            
            Origin = origin;
            Radius = radius;
        }
        
        public static Circle2D operator +(Circle2D self, Vec2D vec) => new(self.Origin + vec, self.Radius);
        public static Circle2D operator +(Circle2D self, Vector2D vec) => new(self.Origin + vec, self.Radius);
        public static Circle2D operator -(Circle2D self, Vec2D vec) => new(self.Origin - vec, self.Radius);
        public static Circle2D operator -(Circle2D self, Vector2D vec) => new(self.Origin - vec, self.Radius);
        public static bool operator ==(Circle2D self, Circle2D other) => self.Radius == other.Radius && self.Origin == other.Origin;
        public static bool operator !=(Circle2D self, Circle2D other) => !(self == other);
        
        public Vec2D ToPoint(double radians) => (Vec2D.UnitCircle(radians) * Radius) + Origin;
        public bool Contains(Vec2D point) => Origin.Distance(point) <= Radius;
        public bool Contains(Vector2D point) => Origin.Distance(point) <= Radius;
        public bool Contains(Vec3D point) => Origin.Distance(point.XY) <= Radius;
        public bool Contains(Vector3D point) => Origin.Distance(point.XY) <= Radius;

        public bool Intersects(Circle2D other)
        {
            double distanceSquared = Origin.DistanceSquared(other.Origin);
            double radSumSquared = (Radius + other.Radius) * (Radius + other.Radius);
            return distanceSquared < radSumSquared;
        }

        public bool Intersects(Seg2D seg)
        {
            // TODO
            return false;
        }
        
        public bool IntersectsAsLine(Seg2D seg)
        {
            // TODO
            return false;
        }
        
        public bool Intersects(Box2D box)
        {
            // TODO
            return false;
        }
        
        public override string ToString() => $"({Origin}), {Radius}";
        public override bool Equals(object? obj) => obj is Circle2D c && Radius == c.Radius && Origin == c.Origin;
        public override int GetHashCode() => HashCode.Combine(Radius.GetHashCode(), Origin.GetHashCode());
    }
}
