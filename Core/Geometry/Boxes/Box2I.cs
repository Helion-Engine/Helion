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
    public readonly struct Box2I
    {
        public static readonly Box2I UnitBox = ((0, 0), (1, 1));

        public readonly Vec2I Min;
        public readonly Vec2I Max;
        public readonly CoordinateSystem CoordinateSystem;

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
        public Dimension Dimension => new(Width, Height);
        public Vec2I Sides => new(Width, Height);

        public Box2I(Vec2I min, Vec2I max)
        {
            Min = min;
            Max = max;
            CoordinateSystem = min.Y <= max.Y ? CoordinateSystem.Cartesian : CoordinateSystem.Image;
        }

        public Box2I(Vec2I min, Vector2I max)
        {
            Min = min;
            Max = max.Struct;
            CoordinateSystem = min.Y <= max.Y ? CoordinateSystem.Cartesian : CoordinateSystem.Image;
        }

        public Box2I(Vector2I min, Vec2I max)
        {
            Min = min.Struct;
            Max = max;
            CoordinateSystem = min.Y <= max.Y ? CoordinateSystem.Cartesian : CoordinateSystem.Image;
        }

        public Box2I(Vector2I min, Vector2I max)
        {
            Min = min.Struct;
            Max = max.Struct;
            CoordinateSystem = min.Y <= max.Y ? CoordinateSystem.Cartesian : CoordinateSystem.Image;
        }

        public Box2I(Vec2I center, int radius)
        {
            Min = new(center.X - radius, center.Y - radius);
            Max = new(center.X + radius, center.Y + radius);
            CoordinateSystem = CoordinateSystem.Cartesian;
        }

        public Box2I(Vector2I center, int radius)
        {
            Min = new(center.X - radius, center.Y - radius);
            Max = new(center.X + radius, center.Y + radius);
            CoordinateSystem = CoordinateSystem.Cartesian;
        }

        public static implicit operator Box2I(ValueTuple<Vec2I, Vec2I> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box2I(ValueTuple<Vec2I, Vector2I> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box2I(ValueTuple<Vector2I, Vec2I> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box2I(ValueTuple<Vector2I, Vector2I> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public void Deconstruct(out Vec2I min, out Vec2I max)
        {
            min = Min;
            max = Max;
        }

        public static Box2I operator +(Box2I self, Vec2I offset) => new(self.Min + offset, self.Max + offset);
        public static Box2I operator +(Box2I self, Vector2I offset) => new(self.Min + offset, self.Max + offset);
        public static Box2I operator -(Box2I self, Vec2I offset) => new(self.Min - offset, self.Max - offset);
        public static Box2I operator -(Box2I self, Vector2I offset) => new(self.Min - offset, self.Max - offset);

    }
}
