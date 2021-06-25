// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry;
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
        public readonly CoordinateSystem CoordinateSystem;

        public Vec2I Min => m_Min;
        public Vec2I Max => m_Max;
        public Vec2I TopLeft => new(Min.X, Max.Y);
        public Vec2I BottomLeft => Min;
        public Vec2I BottomRight => new(Max.X, Min.Y);
        public Vec2I TopRight => Max;
        public int Top => CoordinateSystem == CoordinateSystem.Cartesian ? Max.Y : Min.Y;
        public int Bottom => CoordinateSystem == CoordinateSystem.Cartesian ? Min.Y : Max.Y;
        public int Left => Min.X;
        public int Right => Max.X;
        public int Width => Max.X - Min.X;
        public int Height => CoordinateSystem == CoordinateSystem.Cartesian ? Max.Y - Min.Y : Min.Y - Max.Y;
        public Box2I Struct => new(Min, Max);
        public Dimension Dimension => new(Width, Height);
        public Vec2I Sides => new(Width, Height);

        public BoundingBox2I(Vec2I min, Vec2I max)
        {
            m_Min = min;
            m_Max = max;
            CoordinateSystem = min.Y <= max.Y ? CoordinateSystem.Cartesian : CoordinateSystem.Image;
        }

        public BoundingBox2I(Vec2I min, Vector2I max)
        {
            m_Min = min;
            m_Max = max.Struct;
            CoordinateSystem = min.Y <= max.Y ? CoordinateSystem.Cartesian : CoordinateSystem.Image;
        }

        public BoundingBox2I(Vector2I min, Vec2I max)
        {
            m_Min = min.Struct;
            m_Max = max;
            CoordinateSystem = min.Y <= max.Y ? CoordinateSystem.Cartesian : CoordinateSystem.Image;
        }

        public BoundingBox2I(Vector2I min, Vector2I max)
        {
            m_Min = min.Struct;
            m_Max = max.Struct;
            CoordinateSystem = min.Y <= max.Y ? CoordinateSystem.Cartesian : CoordinateSystem.Image;
        }

        public BoundingBox2I(Vec2I center, int radius)
        {
            m_Min = new(center.X - radius, center.Y - radius);
            m_Max = new(center.X + radius, center.Y + radius);
            CoordinateSystem = CoordinateSystem.Cartesian;
        }

        public BoundingBox2I(Vector2I center, int radius)
        {
            m_Min = new(center.X - radius, center.Y - radius);
            m_Max = new(center.X + radius, center.Y + radius);
            CoordinateSystem = CoordinateSystem.Cartesian;
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

    }
}
