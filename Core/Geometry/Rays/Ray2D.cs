using System;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Spheres;
using Helion.Geometry.Vectors;

namespace Helion.Geometry.Rays
{
    public struct Ray2D
    {
        public Vec2D Origin;
        public Vec2D Direction;

        public Ray2D(Vec2D direction) : this(Vec2D.Zero, direction)
        {
        }
        
        public Ray2D(Vec2D origin, Vec2D direction)
        {
            Origin = origin;
            Direction = direction.Unit();
        }
        
        public static Ray2D operator +(Ray2D self, Vec2D vec) => new(self.Origin + vec, self.Direction);
        public static Ray2D operator +(Ray2D self, Vector2D vec) => new(self.Origin + vec, self.Direction);
        public static Ray2D operator -(Ray2D self, Vec2D vec) => new(self.Origin - vec, self.Direction);
        public static Ray2D operator -(Ray2D self, Vector2D vec) => new(self.Origin - vec, self.Direction);
        public static bool operator ==(Ray2D self, Ray2D other) => self.Origin == other.Origin && self.Direction == other.Direction;
        public static bool operator !=(Ray2D self, Ray2D other) => !(self == other);
        
        public bool Intersects(Ray2D ray)
        {
            // TODO
            return false;
        }
        
        public bool Intersects(Ray2D ray, out double t)
        {
            t = double.NaN;
            
            // TODO
            return false;
        }

        public bool Intersects(Seg2D seg)
        {
            // TODO
            return false;
        }
        
        public bool Intersects(Seg2D seg, out double t)
        {
            t = double.NaN;
            
            // TODO
            return false;
        }
        
        public bool Intersects(Box2D box)
        {
            // TODO
            return false;
        }
        
        public bool Intersects(Box2D box, out double t)
        {
            t = double.NaN;
            
            // TODO
            return false;
        }
        
        public bool Intersects(Circle2D circle)
        {
            // TODO
            return false;
        }
        
        public bool Intersects(Circle2D circle, out double t)
        {
            t = double.NaN;
            
            // TODO
            return false;
        }
        
        public override string ToString() => $"({Origin}), ({Direction})";
        public override bool Equals(object? obj) => obj is Ray2D r && Origin == r.Origin && Direction == r.Direction;
        public override int GetHashCode() => HashCode.Combine(Origin.GetHashCode(), Direction.GetHashCode());
    }
}
