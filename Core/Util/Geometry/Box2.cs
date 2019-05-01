using System.Numerics;

namespace Helion.Util.Geometry
{
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
        /// Creates a box from a bottom left and top right point. It is an 
        /// error if the min has any coordinate greater the maximum point.
        /// </summary>
        /// <param name="min">The bottom left point.</param>
        /// <param name="max">The top right point.</param>
        public Box2F(Vector2 min, Vector2 max)
        {
            Assert.Precondition(min.X <= max.X, "Bounding box min X > max X");
            Assert.Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min;
            Max = max;
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
        public Vector2 Sides() => Max - Min;

        public override string ToString() => $"({Min}), ({Max})";
    }
}
