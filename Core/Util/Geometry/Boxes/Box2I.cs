using System.Diagnostics.Contracts;
using Helion.Geometry;
using Helion.Util.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Geometry.Boxes
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
        [Pure]
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
        [Pure]
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
        [Pure]
        public bool Overlaps(Box2I box)
        {
            return !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
        }

        /// <summary>
        /// Calculates the sides of this bounding box.
        /// </summary>
        /// <returns>The sides of the bounding box.</returns>
        [Pure]
        public Vec2I Sides() => Max - Min;

        public override string ToString() => $"({Min}), ({Max})";
    }
}