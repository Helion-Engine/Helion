using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Helion.Geometry;
using Helion.Util.Geometry.Segments.Enums;
using Helion.Util.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Geometry.Boxes
{
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
        [Pure]
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
        [Pure]
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
        [Pure]
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
        [Pure]
        public bool Intersects(Seg2Fixed seg) => seg.Intersects(this);

        /// <summary>
        /// Calculates the sides of this bounding box.
        /// </summary>
        /// <returns>The sides of the bounding box.</returns>
        [Pure]
        public Vec2Fixed Sides() => Max - Min;

        public override string ToString() => $"({Min}), ({Max})";
    }
}