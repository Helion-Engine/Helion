// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Geometry.Boxes
{
    public class BoundingBox2I
    {
        protected Vec2I m_Min;
        protected Vec2I m_Max;

        public Vec2I Min => m_Min;
        public Vec2I Max => m_Max;
        public Vec2I TopLeft => new(Min.X, Max.Y);
        public Vec2I BottomLeft => Min;
        public Vec2I BottomRight => new(Max.X, Min.Y);
        public Vec2I TopRight => Max;
        public int Top => Max.Y;
        public int Bottom => Min.Y;
        public int Left => Min.X;
        public int Right => Max.X;
        public int Width => Max.X - Min.X;
        public int Height => Max.Y - Min.Y;
        public Box2I Struct => new(Min, Max);
        public Dimension Dimension => new(Width, Height);
        public Vec2I Sides => Max - Min;

        public BoundingBox2I(Vec2I min, Vec2I max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            m_Min = min;
            m_Max = max;
        }

        public BoundingBox2I(Vec2I min, Vector2I max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            m_Min = min;
            m_Max = max.Struct;
        }

        public BoundingBox2I(Vector2I min, Vec2I max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            m_Min = min.Struct;
            m_Max = max;
        }

        public BoundingBox2I(Vector2I min, Vector2I max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            m_Min = min.Struct;
            m_Max = max.Struct;
        }

        public BoundingBox2I(Vec2I center, int radius)
        {
            Precondition(radius >= 0, "Bounding box radius yields min X > max X");

            m_Min = new(center.X - radius, center.Y - radius);
            m_Max = new(center.X + radius, center.Y + radius);
        }

        public BoundingBox2I(Vector2I center, int radius)
        {
            Precondition(radius >= 0, "Bounding box radius yields min X > max X");

            m_Min = new(center.X - radius, center.Y - radius);
            m_Max = new(center.X + radius, center.Y + radius);
        }

        public void Deconstruct(out Vec2I min, out Vec2I max)
        {
            min = Min;
            max = Max;
        }

        public static Box2I operator +(BoundingBox2I self, Vec2I offset) => new(self.Min + offset, self.Max + offset);
        public static Box2I operator +(BoundingBox2I self, Vector2I offset) => new(self.Min + offset, self.Max + offset);
        public static Box2I operator -(BoundingBox2I self, Vec2I offset) => new(self.Min - offset, self.Max - offset);
        public static Box2I operator -(BoundingBox2I self, Vector2I offset) => new(self.Min - offset, self.Max - offset);

        public bool Contains(Vec2I point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Contains(Vector2I point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Contains(Vec3I point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Contains(Vector3I point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        public bool Overlaps(in Box2I box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        public bool Overlaps(BoundingBox2I box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        public Box2I Combine(params Box2I[] boxes)
        {
            Vec2I min = Min;
            Vec2I max = Max;
            for (int i = 0; i < boxes.Length; i++)
            {
                Box2I box = boxes[i];
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
            }
            return new(min, max);
        }
        public Box2I Combine(params BoundingBox2I[] boxes)
        {
            Vec2I min = Min;
            Vec2I max = Max;
            for (int i = 0; i < boxes.Length; i++)
            {
                BoundingBox2I box = boxes[i];
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
            }
            return new(min, max);
        }
        public override string ToString() => $"({Min}), ({Max})";
    }
}
