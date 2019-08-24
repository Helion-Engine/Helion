using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Geometry
{
    /// <summary>
    /// A two dimensional box, which follows the cartesian coordinate system.
    /// </summary>
    public struct Box2I
    {
        /// <summary>
        /// The minimum point in the box. This is equal to the bottom left 
        /// corner.
        /// </summary>
        public Vec2I Min;

        /// <summary>
        /// The maximum point in the box. This is equal to the top right 
        /// corner.
        /// </summary>
        public Vec2I Max;

        /// <summary>
        /// The top left corner of the box.
        /// </summary>
        public Vec2I TopLeft => new Vec2I(Min.X, Max.Y);

        /// <summary>
        /// The bottom left corner of the box.
        /// </summary>
        public Vec2I BottomLeft => Min;

        /// <summary>
        /// The bottom right corner of the box.
        /// </summary>
        public Vec2I BottomRight => new Vec2I(Max.X, Min.Y);

        /// <summary>
        /// The top right corner of the box.
        /// </summary>
        public Vec2I TopRight => Max;

        /// <summary>
        /// The top value of the box.
        /// </summary>
        public int Top => Max.Y;
        
        /// <summary>
        /// The bottom value of the box.
        /// </summary>
        public int Bottom => Min.Y;
        
        /// <summary>
        /// The left value of the box.
        /// </summary>
        public int Left => Min.X;
        
        /// <summary>
        /// The right value of the box.
        /// </summary>
        public int Right => Max.X;
        
        /// <summary>
        /// A property that calculates the width of the box.
        /// </summary>
        public int Width => Max.X - Min.X;
        
        /// <summary>
        /// A property that calculates the height of the box.
        /// </summary>
        public int Height => Max.Y - Min.Y;
        
        /// <summary>
        /// A property that calculates the dimension of the box.
        /// </summary>
        public Dimension Dimension => new Dimension(Width, Height);

        /// <summary>
        /// Creates a box from a bottom left and top right point. It is an 
        /// error if the min has any coordinate greater the maximum point.
        /// </summary>
        /// <param name="min">The bottom left point.</param>
        /// <param name="max">The top right point.</param>
        public Box2I(Vec2I min, Vec2I max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min;
            Max = max;
        }

        /// <summary>
        /// Checks if the box contains the point. Being on the edge is not
        /// considered to be containing.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if it is inside, false if not.</returns>
        public bool Contains(Vec2I point)
        {
            return point.X <= Min.X || point.X >= Max.X || point.Y <= Min.Y || point.Y >= Max.Y;
        }

        /// <summary>
        /// Checks if the box contains the point. Being on the edge is not
        /// considered to be containing.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if it is inside, false if not.</returns>
        public bool Contains(Vec3I point)
        {
            return point.X <= Min.X || point.X >= Max.X || point.Y <= Min.Y || point.Y >= Max.Y;
        }

        /// <summary>
        /// Checks if the boxes overlap. Touching is not considered to be
        /// overlapping.
        /// </summary>
        /// <param name="box">The other box to check against.</param>
        /// <returns>True if they overlap, false if not.</returns>
        public bool Overlaps(Box2I box)
        {
            return !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        }

        /// <summary>
        /// Calculates the sides of this bounding box.
        /// </summary>
        /// <returns>The sides of the bounding box.</returns>
        public Vec2I Sides() => Max - Min;

        public override string ToString() => $"({Min}), ({Max})";
    }
       
    /// <summary>
    /// A two dimensional box, which follows the cartesian coordinate system.
    /// </summary>
    public struct Box2F
    {
        /// <summary>
        /// The minimum point in the box. This is equal to the bottom left 
        /// corner.
        /// </summary>
        public Vector2 Min;

        /// <summary>
        /// The maximum point in the box. This is equal to the top right 
        /// corner.
        /// </summary>
        public Vector2 Max;

        /// <summary>
        /// The top left corner of the box.
        /// </summary>
        public Vector2 TopLeft => new Vector2(Min.X, Max.Y);

        /// <summary>
        /// The bottom left corner of the box.
        /// </summary>
        public Vector2 BottomLeft => Min;

        /// <summary>
        /// The bottom right corner of the box.
        /// </summary>
        public Vector2 BottomRight => new Vector2(Max.X, Min.Y);

        /// <summary>
        /// The top right corner of the box.
        /// </summary>
        public Vector2 TopRight => Max;

        /// <summary>
        /// The top value of the box.
        /// </summary>
        public float Top => Max.Y;
        
        /// <summary>
        /// The bottom value of the box.
        /// </summary>
        public float Bottom => Min.Y;
        
        /// <summary>
        /// The left value of the box.
        /// </summary>
        public float Left => Min.X;
        
        /// <summary>
        /// The right value of the box.
        /// </summary>
        public float Right => Max.X;
                
        /// <summary>
        /// A property that calculates the width of the box.
        /// </summary>
        public float Width => Max.X - Min.X;
        
        /// <summary>
        /// A property that calculates the height of the box.
        /// </summary>
        public float Height => Max.Y - Min.Y;

        /// <summary>
        /// Creates a box from a bottom left and top right point. It is an 
        /// error if the min has any coordinate greater the maximum point.
        /// </summary>
        /// <param name="min">The bottom left point.</param>
        /// <param name="max">The top right point.</param>
        public Box2F(Vector2 min, Vector2 max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min;
            Max = max;
        }
        
        /// <summary>
        /// Creates a bigger box from a series of smaller boxes, returning such
        /// a box that encapsulates minimally all the provided arguments.
        /// </summary>
        /// <param name="firstBox">The first box in the sequence.</param>
        /// <param name="boxes">The remaining boxes, if any.</param>
        /// <returns>A box that encases all of the args tightly.</returns>
        public static Box2F Combine(Box2F firstBox, params Box2F[] boxes)
        {
            Vector2 min = firstBox.Min;
            Vector2 max = firstBox.Max;

            foreach (Box2F box in boxes)
            {
                min.X = Math.Min(min.X, box.Min.X);
                min.Y = Math.Min(min.Y, box.Min.Y);
                max.X = Math.Max(max.X, box.Max.X);
                max.Y = Math.Max(max.Y, box.Max.Y);
            }

            return new Box2F(min, max);
        }

        /// <summary>
        /// Bounds all the segments by a tight axis aligned bounding box.
        /// </summary>
        /// <param name="segments">The segments to bound. This should contain
        /// at least one element.</param>
        /// <returns>A box that bounds the segments.</returns>
        public static Box2F BoundSegments(List<Seg2F> segments)
        {
            Precondition(segments.Count > 0, "Cannot bound segments when none are provided");

            return Combine(segments.First().Box, segments.Skip(1).Select(s => s.Box).ToArray());
        }

        /// <summary>
        /// Checks if the box contains the point. Being on the edge is not
        /// considered to be containing.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if it is inside, false if not.</returns>
        public bool Contains(Vector2 point)
        {
            return point.X <= Min.X || point.X >= Max.X || point.Y <= Min.Y || point.Y >= Max.Y;
        }

        /// <summary>
        /// Checks if the box contains the point. Being on the edge is not
        /// considered to be containing.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if it is inside, false if not.</returns>
        public bool Contains(Vector3 point)
        {
            return point.X <= Min.X || point.X >= Max.X || point.Y <= Min.Y || point.Y >= Max.Y;
        }

        /// <summary>
        /// Checks if the boxes overlap. Touching is not considered to be
        /// overlapping.
        /// </summary>
        /// <param name="box">The other box to check against.</param>
        /// <returns>True if they overlap, false if not.</returns>
        public bool Overlaps(Box2F box)
        {
            return !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        }

        /// <summary>
        /// Checks for an intersection with a segment. This will invoke
        /// <see cref="Seg2F.Intersects(Box2F)"/>.
        /// </summary>
        /// <param name="seg">The seg to check against.</param>
        /// <returns>True if it intersects, false if not.</returns>
        public bool Intersects(Seg2F seg) => seg.Intersects(this);

        /// <summary>
        /// Calculates the sides of this bounding box.
        /// </summary>
        /// <returns>The sides of the bounding box.</returns>
        public Vector2 Sides => Max - Min;

        public override string ToString() => $"({Min}), ({Max})";
    }

    /// <summary>
    /// A two dimensional box, which follows the cartesian coordinate system.
    /// </summary>
    public struct Box2D
    {
        /// <summary>
        /// The minimum point in the box. This is equal to the bottom left 
        /// corner.
        /// </summary>
        public Vec2D Min;

        /// <summary>
        /// The maximum point in the box. This is equal to the top right 
        /// corner.
        /// </summary>
        public Vec2D Max;

        /// <summary>
        /// The top left corner of the box.
        /// </summary>
        public Vec2D TopLeft => new Vec2D(Min.X, Max.Y);

        /// <summary>
        /// The bottom left corner of the box.
        /// </summary>
        public Vec2D BottomLeft => Min;

        /// <summary>
        /// The bottom right corner of the box.
        /// </summary>
        public Vec2D BottomRight => new Vec2D(Max.X, Min.Y);

        /// <summary>
        /// The top right corner of the box.
        /// </summary>
        public Vec2D TopRight => Max;
        
        /// <summary>
        /// The top value of the box.
        /// </summary>
        public double Top => Max.Y;
        
        /// <summary>
        /// The bottom value of the box.
        /// </summary>
        public double Bottom => Min.Y;
        
        /// <summary>
        /// The left value of the box.
        /// </summary>
        public double Left => Min.X;
        
        /// <summary>
        /// The right value of the box.
        /// </summary>
        public double Right => Max.X;
                
        /// <summary>
        /// A property that calculates the width of the box.
        /// </summary>
        public double Width => Max.X - Min.X;
        
        /// <summary>
        /// A property that calculates the height of the box.
        /// </summary>
        public double Height => Max.Y - Min.Y;

        /// <summary>
        /// Creates a box from a bottom left and top right point. It is an 
        /// error if the min has any coordinate greater the maximum point.
        /// </summary>
        /// <param name="min">The bottom left point.</param>
        /// <param name="max">The top right point.</param>
        public Box2D(Vec2D min, Vec2D max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min;
            Max = max;
        }
        
        /// <summary>
        /// Creates a bigger box from a series of smaller boxes, returning such
        /// a box that encapsulates minimally all the provided arguments.
        /// </summary>
        /// <param name="firstBox">The first box in the sequence.</param>
        /// <param name="boxes">The remaining boxes, if any.</param>
        /// <returns>A box that encases all of the args tightly.</returns>
        public static Box2D Combine(Box2D firstBox, params Box2D[] boxes)
        {
            Vec2D min = firstBox.Min;
            Vec2D max = firstBox.Max;

            foreach (Box2D box in boxes)
            {
                min.X = Math.Min(min.X, box.Min.X);
                min.Y = Math.Min(min.Y, box.Min.Y);
                max.X = Math.Max(max.X, box.Max.X);
                max.Y = Math.Max(max.Y, box.Max.Y);
            }

            return new Box2D(min, max);
        }

        /// <summary>
        /// Bounds all the segments by a tight axis aligned bounding box.
        /// </summary>
        /// <param name="segments">The segments to bound. This should contain
        /// at least one element.</param>
        /// <returns>A box that bounds the segments.</returns>
        public static Box2D BoundSegments(List<Seg2D> segments)
        {
            Precondition(segments.Count > 0, "Cannot bound segments when none are provided");

            return Combine(segments.First().Box, segments.Skip(1).Select(s => s.Box).ToArray());
        }
        
        /// <summary>
        /// Takes some offset delta and creates a copy of the box at the offset
        /// provided.
        /// </summary>
        /// <param name="offset">The absolute center coordinate.</param>
        /// <param name="radius">The radius of the box.</param>
        /// <returns>The box at the position.</returns>
        public static Box2D CopyToOffset(Vec2D offset, double radius)
        {
            Vec2D newMin = new Vec2D(offset.X - radius, offset.Y - radius);
            Vec2D newMax = new Vec2D(offset.X + radius, offset.Y + radius);
            return new Box2D(newMin, newMax);
        }

        /// <summary>
        /// Checks if the box contains the point. Being on the edge is not
        /// considered to be containing.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if it is inside, false if not.</returns>
        [Pure]
        public bool Contains(Vec2D point)
        {
            return point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        }

        /// <summary>
        /// Checks if the box contains the point. Being on the edge is not
        /// considered to be containing.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if it is inside, false if not.</returns>
        [Pure]
        public bool Contains(Vector3 point) 
        {
            return point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
        }

        /// <summary>
        /// Checks if the boxes overlap. Touching is not considered to be
        /// overlapping.
        /// </summary>
        /// <param name="box">The other box to check against.</param>
        /// <returns>True if they overlap, false if not.</returns>
        [Pure]
        public bool Overlaps(Box2D box)
        {
            return !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        }

        /// <summary>
        /// Checks for an intersection with a segment. This will invoke
        /// <see cref="Seg2D.Intersects(Box2D)"/>.
        /// </summary>
        /// <param name="seg">The seg to check against.</param>
        /// <returns>True if it intersects, false if not.</returns>
        [Pure]
        public bool Intersects(Seg2D seg) => seg.Intersects(this);

        /// <summary>
        /// Gets the closest edge to some point. If along diagonals, it instead
        /// will return the diagonal.
        /// </summary>
        /// <param name="position">The position to look from.</param>
        /// <returns>The start and end points along the bounding box.</returns>
        [Pure]
        public (Vec2D, Vec2D) GetSpanningEdge(in Vec2D position)
        {
            // This is best understood by asking ourselves how we'd classify
            // where we are along a 1D line. Suppose we want to find out which
            // one of the spans were in along the X axis:
            //
            //      0     1     2
            //   A-----B-----C-----D
            //
            // We want to know if we're in span 0, 1, or 2. We can just check
            // by doing `if x > B` for span 1 or 2, and `if x > C` for span 2.
            // Instead of doing if statements, we can just cast the bool to an
            // int and add them up.
            //
            // Next we do this along the Y axis.
            //
            // After our results, we can merge the bits such that the higher
            // two bits are the Y value, and the lower 2 bits are the X value.
            // This gives us: 0bYYXX.
            //
            // Since each coordinate in the following image has its own unique
            // bitcode, we can switch on the bitcode to get the corners.
            //
            //       XY values           Binary codes 
            //
            //      02 | 12 | 22       1000|1001|1010 
            //         |    |           8  | 9  | A   
            //     ----o----o----      ----o----o---- 
            //      01 | 11 | 21       0100|0101|0110 
            //         |    |           4  | 5  | 6   
            //     ----o----o----      ----o----o---- 
            //      00 | 10 | 20       0000|0001|0010 
            //         |    |           0  | 1  | 2   
            //
            // Note this is my optimization to the Cohen-Sutherland algorithm
            // bitcode detector.
            uint horizontalBits = Convert.ToUInt32(position.X > Left) + Convert.ToUInt32(position.X > Right);
            uint verticalBits = Convert.ToUInt32(position.Y > Bottom) + Convert.ToUInt32(position.Y > Top);

            switch (horizontalBits | (verticalBits << 2))
            {
            case 0x0: // Bottom left
                return (TopLeft, BottomRight);
            case 0x1: // Bottom middle
                return (BottomLeft, BottomRight);
            case 0x2: // Bottom right
                return (BottomLeft, TopRight);
            case 0x4: // Middle left
                return (TopLeft, BottomLeft);
            case 0x5: // Center (this shouldn't be a case via precondition).
                return (TopLeft, BottomRight);
            case 0x6: // Middle right
                return (BottomRight, TopRight);
            case 0x8: // Top left
                return (TopRight, BottomLeft);
            case 0x9: // Top middle
                return (TopRight, TopLeft);
            case 0xA: // Top right
                return (BottomRight, TopLeft);
            default:
                Fail("Unexpected spanning edge bit code");
                return (TopLeft, BottomRight);
            }
        }

        /// <summary>
        /// Calculates the sides of this bounding box.
        /// </summary>
        /// <returns>The sides of the bounding box.</returns>
        public Vec2D Sides() => Max - Min;

        public override string ToString() => $"({Min}), ({Max})";
    }

    /// <summary>
    /// A two dimensional box, which follows the cartesian coordinate system.
    /// </summary>
    public struct Box2Fixed
    {
        /// <summary>
        /// The minimum point in the box. This is equal to the bottom left 
        /// corner.
        /// </summary>
        public Vec2Fixed Min;

        /// <summary>
        /// The maximum point in the box. This is equal to the top right 
        /// corner.
        /// </summary>
        public Vec2Fixed Max;

        /// <summary>
        /// The top left corner of the box.
        /// </summary>
        public Vec2Fixed TopLeft => new Vec2Fixed(Min.X, Max.Y);

        /// <summary>
        /// The bottom left corner of the box.
        /// </summary>
        public Vec2Fixed BottomLeft => Min;

        /// <summary>
        /// The bottom right corner of the box.
        /// </summary>
        public Vec2Fixed BottomRight => new Vec2Fixed(Max.X, Min.Y);

        /// <summary>
        /// The top right corner of the box.
        /// </summary>
        public Vec2Fixed TopRight => Max;
        
        /// <summary>
        /// The top value of the box.
        /// </summary>
        public Fixed Top => Max.Y;
        
        /// <summary>
        /// The bottom value of the box.
        /// </summary>
        public Fixed Bottom => Min.Y;
        
        /// <summary>
        /// The left value of the box.
        /// </summary>
        public Fixed Left => Min.X;
        
        /// <summary>
        /// The right value of the box.
        /// </summary>
        public Fixed Right => Max.X;
                
        /// <summary>
        /// A property that calculates the width of the box.
        /// </summary>
        public Fixed Width => Max.X - Min.X;
        
        /// <summary>
        /// A property that calculates the height of the box.
        /// </summary>
        public Fixed Height => Max.Y - Min.Y;

        /// <summary>
        /// Creates a box from a bottom left and top right point. It is an 
        /// error if the min has any coordinate greater the maximum point.
        /// </summary>
        /// <param name="min">The bottom left point.</param>
        /// <param name="max">The top right point.</param>
        public Box2Fixed(Vec2Fixed min, Vec2Fixed max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min;
            Max = max;
        }

        /// <summary>
        /// Creates a bigger box from a series of smaller boxes, returning such
        /// a box that encapsulates minimally all the provided arguments.
        /// </summary>
        /// <param name="firstBox">The first box in the sequence.</param>
        /// <param name="boxes">The remaining boxes, if any.</param>
        /// <returns>A box that encases all of the args tightly.</returns>
        public static Box2Fixed Combine(Box2Fixed firstBox, params Box2Fixed[] boxes)
        {
            Vec2Fixed min = firstBox.Min;
            Vec2Fixed max = firstBox.Max;

            foreach (Box2Fixed box in boxes)
            {
                min.X = MathHelper.Min(min.X, box.Min.X);
                min.Y = MathHelper.Min(min.Y, box.Min.Y);
                max.X = MathHelper.Max(max.X, box.Max.X);
                max.Y = MathHelper.Max(max.Y, box.Max.Y);
            }

            return new Box2Fixed(min, max);
        }

        /// <summary>
        /// Bounds all the segments by a tight axis aligned bounding box.
        /// </summary>
        /// <param name="segments">The segments to bound. This should contain
        /// at least one element.</param>
        /// <returns>A box that bounds the segments.</returns>
        public static Box2Fixed BoundSegments(List<Seg2Fixed> segments)
        {
            Precondition(segments.Count > 0, "Cannot bound segments when none are provided");

            return Combine(segments.First().Box, segments.Skip(1).Select(s => s.Box).ToArray());
        }

        /// <summary>
        /// Checks if the box contains the point. Being on the edge is not
        /// considered to be containing.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if it is inside, false if not.</returns>
        public bool Contains(Vec2Fixed point)
        {
            return point.X <= Min.X || point.X >= Max.X || point.Y <= Min.Y || point.Y >= Max.Y;
        }

        /// <summary>
        /// Checks if the box contains the point. Being on the edge is not
        /// considered to be containing.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if it is inside, false if not.</returns>
        public bool Contains(Vec3Fixed point)
        {
            return point.X <= Min.X || point.X >= Max.X || point.Y <= Min.Y || point.Y >= Max.Y;
        }

        /// <summary>
        /// Checks if the boxes overlap. Touching is not considered to be
        /// overlapping.
        /// </summary>
        /// <param name="box">The other box to check against.</param>
        /// <returns>True if they overlap, false if not.</returns>
        public bool Overlaps(Box2Fixed box)
        {
            return !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        }

        /// <summary>
        /// Checks for an intersection with a segment. This will invoke
        /// <see cref="Seg2Fixed.Intersects(Box2Fixed)"/>.
        /// </summary>
        /// <param name="seg">The seg to check against.</param>
        /// <returns>True if it intersects, false if not.</returns>
        public bool Intersects(Seg2Fixed seg) => seg.Intersects(this);

        /// <summary>
        /// Calculates the sides of this bounding box.
        /// </summary>
        /// <returns>The sides of the bounding box.</returns>
        public Vec2Fixed Sides() => Max - Min;

        public override string ToString() => $"({Min}), ({Max})";
    }
}