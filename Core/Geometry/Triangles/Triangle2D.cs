using System;
using System.Collections.Generic;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Spheres;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;

namespace Helion.Geometry.Triangles
{
    public struct Triangle2D
    {
        public Vec2D First;
        public Vec2D Second;
        public Vec2D Third;

        public Box2D Box => MakeBox();
        public IEnumerable<Vec2D> Vertices => GetVertices();

        public Triangle2D(Vec2D first, Vec2D second, Vec2D third)
        {
            First = first;
            Second = second;
            Third = third;
        }

        public static Triangle2D operator +(Triangle2D self, Vec2D other) => new(self.First + other, self.Second + other, self.Third + other);
        public static Triangle2D operator +(Triangle2D self, Vector2D other) => new(self.First + other, self.Second + other, self.Third + other);
        public static Triangle2D operator -(Triangle2D self, Vec2D other) => new(self.First - other, self.Second - other, self.Third - other);
        public static Triangle2D operator -(Triangle2D self, Vector2D other) => new(self.First - other, self.Second - other, self.Third - other);
        public static bool operator ==(Triangle2D self, Triangle2D other) => self.First == other.First && self.Second == other.Second && self.Third == other.Third;
        public static bool operator !=(Triangle2D self, Triangle2D other) => !(self == other);
        
        public bool Contains(Vec2D point)
        {
            // TODO
            return false;
        }
        
        public bool Contains(Vector2D point)
        {
            // TODO
            return false;
        }
        
        public bool Contains(Vec3D point) => Contains(point.XY);

        public bool Contains(Vector3D point) => Contains(point.XY);

        public bool Intersects(Seg2D seg)
        {
            // TODO
            return false;
        }
        
        public bool Intersects(Box2D box)
        {
            // TODO
            return false;
        }
        
        public bool Intersects(Circle2D circle)
        {
            // TODO
            return false;
        }
        
        public override string ToString() => $"({First}), ({Second}), ({Third})";
        public override bool Equals(object? obj) => obj is Triangle2D tri && First == tri.First && Second == tri.Second && Third == tri.Third;
        public override int GetHashCode() => HashCode.Combine(First.GetHashCode(), Second.GetHashCode(), Third.GetHashCode());

        private Box2D MakeBox()
        {
            double minX = First.X.Min(Second.X).Min(Third.X);
            double minY = First.Y.Min(Second.Y).Min(Third.Y);
            double maxX = First.X.Max(Second.X).Max(Third.X);
            double maxY = First.Y.Max(Second.Y).Max(Third.Y);
            return new Box2D((minX, minY), (maxX, maxY));
        }
        
        private IEnumerable<Vec2D> GetVertices()
        {
            yield return First;
            yield return Second;
            yield return Third;
        }
    }
}
