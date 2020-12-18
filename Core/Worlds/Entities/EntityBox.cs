using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Vectors;

namespace Helion.Worlds.Entities
{
    /// <summary>
    /// A 3D axis aligned bounding box around an entity. Has convenience
    /// methods for dealing with box movement.
    /// </summary>
    public struct EntityBox
    {
        private Box3D m_box;
        private Vec3D m_centerBottom;
        private double m_radius;

        /// <summary>
        /// The minimum point in the box. This is equal to the bottom left
        /// corner.
        /// </summary>
        public Vec3D Min => m_box.Min;

        /// <summary>
        /// The maximum point in the box. This is equal to the top right
        /// corner.
        /// </summary>
        public Vec3D Max => m_box.Max;

        /// <summary>
        /// The top value of the box.
        /// </summary>
        public double Top => m_box.Max.Z;

        /// <summary>
        /// The bottom value of the box.
        /// </summary>
        public double Bottom => m_box.Min.Z;

        /// <summary>
        /// Gets the position of the actor in the world, which is equal to the
        /// center of the body but at the feet.
        /// </summary>
        public Vec3D Position => m_centerBottom;

        public double Height { get; private set; }

        /// <summary>
        /// Creates a bounding box at the center bottom position provided.
        /// </summary>
        /// <param name="centerBottom">The center bottom position of the box.
        /// </param>
        /// <param name="radius">The radius of the box.</param>
        /// <param name="height">The height of the box.</param>
        public EntityBox(in Vec3D centerBottom, double radius, double height)
        {
            m_box = CreateBoxAtCenterBottom(centerBottom, radius, height);
            m_centerBottom = centerBottom;
            m_radius = radius;
            Height = height;
        }

        /// <summary>
        /// Moves the entire box to the provided position.
        /// </summary>
        /// <param name="centerBottomPosition">The position, which is the
        /// center bottom point.</param>
        public void MoveTo(in Vec3D centerBottomPosition)
        {
            m_centerBottom = centerBottomPosition;
            m_box = CreateBoxAtCenterBottom(centerBottomPosition, m_radius, Height);
        }

        /// <summary>
        /// Sets the Z values of the box to the location provided.
        /// </summary>
        /// <param name="bottomZ">The Z value which is at the bottom of the box
        /// (or in player terms, the feet).</param>
        public void SetZ(double bottomZ)
        {
            m_centerBottom.Z = bottomZ;
            m_box.Min.Z = bottomZ;
            m_box.Max.Z = bottomZ + Height;
        }

        /// <summary>
        /// Sets the position to the new location provided.
        /// </summary>
        /// <param name="position">The position to set to.</param>
        public void SetXY(in Vec2D position)
        {
            m_centerBottom.X = position.X;
            m_centerBottom.Y = position.Y;

            m_box.Min.X = position.X - m_radius;
            m_box.Max.X = position.X + m_radius;
            m_box.Min.Y = position.Y - m_radius;
            m_box.Max.Y = position.Y + m_radius;
        }

        /// <summary>
        /// Sets the height of the box.
        /// </summary>
        /// <param name="height">The height to set.</param>
        public void SetHeight(double height)
        {
            Height = height;
            m_box.Max.Z = m_box.Min.Z + height;
        }

        /// <summary>
        /// Gets the 2D version of the box.
        /// </summary>
        /// <returns>A 2D version of the box.</returns>
        public Box2D To2D() => m_box.To2D();

        /// <summary>
        /// Checks if the boxes overlap. Touching is not considered to be
        /// overlapping.
        /// </summary>
        /// <param name="other">The other entity to check against.</param>
        /// <returns>True if they overlap, false if not.</returns>
        public bool Overlaps(in EntityBox other) => m_box.Overlaps(other.m_box);

        /// <summary>
        /// Checks if the boxes overlap using X/Y coordinates only. Touching is not considered to be
        /// overlapping.
        /// </summary>
        /// <param name="other">The other entity to check against.</param>
        /// <returns>True if they overlap on X/Y, false if not.</returns>
        public bool Overlaps2D(in EntityBox other)
        {
            // This is the same as Box2 Overlaps
            // This is for performance so we do not have a construct a new Box2D everytime we want to check 2D bounds on a 3D box
            return !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
        }

        /// <summary>
        /// Checks if the boxes overlap using X/Y coordinates only. Touching is not considered to be
        /// overlapping.
        /// </summary>
        /// <param name="other">The other Box2d to check against.</param>
        /// <returns>True if they overlap on X/Y, false if not.</returns>
        public bool Overlaps2D(in Box2D other)
        {
            // Same as previous Overlaps2d, convienence function to not construct more Box2D objects
            return !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
        }

        /// <summary>
        /// Checks to see if the two boxes have overlapping Z values. It does
        /// not include touching, it has to be strictly overlapping.
        /// </summary>
        /// <param name="box">The other box to check.</param>
        /// <returns>True if there is some overlap, false if not.</returns>
        public bool OverlapsZ(in EntityBox box) => Top > box.Bottom && Bottom < box.Top;

        /// <inheritdoc/>
        public override string ToString() => $"{m_box}";

        private static Box3D CreateBoxAtCenterBottom(in Vec3D centerBottom, double radius, double height)
        {
            Vec3D min = new Vec3D(centerBottom.X - radius, centerBottom.Y - radius, centerBottom.Z);
            Vec3D max = new Vec3D(centerBottom.X + radius, centerBottom.Y + radius, centerBottom.Z + height);
            return new Box3D(min, max);
        }

        public bool Intersects(in Vec2D p1, in Vec2D p2, ref Vec2D intersect)
        {
            if (p2.X < Min.X && p1.X < Min.X)
                return false;
            if (p2.X > Max.X && p1.X > Max.X)
                return false;
            if (p2.Y < Min.Y && p1.Y < Min.Y)
                return false;
            if (p2.Y > Max.Y && p1.Y > Max.Y)
                return false;
            if (p1.X > Min.X && p1.X < Max.X &&
                p1.Y > Min.Y && p1.Y < Max.Y)
            {
                intersect = p1;
                return true;
            }

            if ((p1.X < Min.X && Intersects(p1.X - Min.X, p2.X - Min.X, p1, p2, ref intersect) && intersect.Y > Min.Y && intersect.Y < Max.Y)
                  || (p1.Y < Min.Y && Intersects(p1.Y - Min.Y, p2.Y - Min.Y, p1, p2, ref intersect) && intersect.X > Min.X && intersect.X < Max.X)
                  || (p1.X > Max.X && Intersects(p1.X - Max.X, p2.X - Max.X, p1, p2, ref intersect) && intersect.Y > Min.Y && intersect.Y < Max.Y)
                  || (p1.Y > Max.Y && Intersects(p1.Y - Max.Y, p2.Y - Max.Y, p1, p2, ref intersect) && intersect.X > Min.X && intersect.X < Max.X))
                return true;

            return false;
        }

        public bool Intersects(in Vec3D p1, in Vec3D p2, ref Vec3D intersect)
        {
            if (p2.X < Min.X && p1.X < Min.X)
                return false;
            if (p2.X > Max.X && p1.X > Max.X)
                return false;
            if (p2.Y < Min.Y && p1.Y < Min.Y)
                return false;
            if (p2.Y > Max.Y && p1.Y > Max.Y)
                return false;
            if (p2.Z < Min.Z && p1.Z < Min.Z)
                return false;
            if (p2.Z > Max.Z && p1.Z > Max.Z)
                return false;
            if (p1.X > Min.X && p1.X < Max.X &&
                p1.Y > Min.Y && p1.Y < Max.Y &&
                p1.Z > Min.Z && p1.Z < Max.Z)
            {
                intersect = p1;
                return true;
            }

            if ((p1.X < Min.X && Intersects(p1.X - Min.X, p2.X - Min.X, p1, p2, ref intersect) && intersect.Y > Min.Y && intersect.Y < Max.Y && intersect.Z > Min.Z && intersect.Z < Max.Z)
                  || (p1.Y < Min.Y && Intersects(p1.Y - Min.Y, p2.Y - Min.Y, p1, p2, ref intersect) && intersect.X > Min.X && intersect.X < Max.X && intersect.Z > Min.Z && intersect.Z < Max.Z)
                  || (p1.Z < Min.Z && Intersects(p1.Z - Min.Z, p2.Z - Min.Z, p1, p2, ref intersect) && intersect.X > Min.X && intersect.X < Max.X && intersect.Y > Min.Y && intersect.Y < Max.Y)
                  || (p1.X > Max.X && Intersects(p1.X - Max.X, p2.X - Max.X, p1, p2, ref intersect) && intersect.Y > Min.Y && intersect.Y < Max.Y && intersect.Z > Min.Z && intersect.Z < Max.Z)
                  || (p1.Y > Max.Y && Intersects(p1.Y - Max.Y, p2.Y - Max.Y, p1, p2, ref intersect) && intersect.X > Min.X && intersect.X < Max.X && intersect.Z > Min.Z && intersect.Z < Max.Z)
                  || (p1.Z > Max.Z && Intersects(p1.Z - Max.Z, p2.Z - Max.Z, p1, p2, ref intersect) && intersect.X > Min.X && intersect.X < Max.X && intersect.Y > Min.Y && intersect.Y < Max.Y))
                return true;

            return false;
        }

        private bool Intersects(double dist1, double dist2, in Vec2D p1, in Vec2D p2, ref Vec2D intersect)
        {
            if (dist1 * dist2 >= 0.0)
                return false;
            if (dist1 == dist2)
                return false;

            intersect = p1 + ((p2 - p1) * (-dist1 / (dist2 - dist1)));
            return true;
        }

        private bool Intersects(double dist1, double dist2, in Vec3D p1, in Vec3D p2, ref Vec3D intersect)
        {
            if (dist1 * dist2 >= 0.0)
                return false;
            if (dist1 == dist2)
                return false;

            intersect = p1 + ((p2 - p1) * (-dist1 / (dist2 - dist1)));
            return true;
        }
    }
}