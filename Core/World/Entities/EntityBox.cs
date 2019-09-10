using Helion.Util.Geometry;
using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Vectors;

namespace Helion.World.Entities
{
    /// <summary>
    /// A 3D axis aligned bounding box around an entity. Has convenience
    /// methods for dealing with box movement.
    /// </summary>
    public struct EntityBox
    {
        private Box3D m_box;
        private Vec3D m_centerBottom;
        private double m_height;
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

        /// <summary>
        /// Creates a bounding box at the center bottom position provided.
        /// </summary>
        /// <param name="centerBottom">The center bottom position of the box.
        /// </param>
        /// <param name="radius">The radius of the box.</param>
        /// <param name="height">The height of the box.</param>
        public EntityBox(Vec3D centerBottom, double radius, double height)
        {
            m_box = CreateBoxAtCenterBottom(centerBottom, radius, height);
            m_centerBottom = centerBottom;
            m_height = height;
            m_radius = radius;
        }

        /// <summary>
        /// Moves the entire box to the provided position.
        /// </summary>
        /// <param name="centerBottomPosition">The position, which is the
        /// center bottom point.</param>
        public void MoveTo(Vec3D centerBottomPosition)
        {
            m_centerBottom = centerBottomPosition;
            m_box = CreateBoxAtCenterBottom(centerBottomPosition, m_radius, m_height);
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
            m_box.Max.Z = bottomZ + m_height;
        }

        /// <summary>
        /// Sets the position to the new location provided.
        /// </summary>
        /// <param name="position">The position to set to.</param>
        public void SetXY(Vec2D position)
        {
            m_centerBottom.X = position.X;
            m_centerBottom.Y = position.Y;
            
            m_box.Min.X = position.X - m_radius;
            m_box.Max.X = position.X + m_radius;
            m_box.Min.Y = position.Y - m_radius;
            m_box.Max.Y = position.Y + m_radius;
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
        public bool Overlaps(EntityBox other) => m_box.Overlaps(other.m_box);

        /// <summary>
        /// Checks if the boxes overlap using X/Y coordinates only. Touching is not considered to be
        /// overlapping.
        /// </summary>
        /// <param name="other">The other entity to check against.</param>
        /// <returns>True if they overlap on X/Y, false if not.</returns>
        public bool Overlaps2D(EntityBox other)
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
        public bool Overlaps2D(Box2D other)
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
        public bool OverlapsZ(EntityBox box) => Top > box.Bottom && Bottom < box.Top;

        /// <inheritdoc/>
        public override string ToString() => $"{m_box}";

        private static Box3D CreateBoxAtCenterBottom(Vec3D centerBottom, double radius, double height)
        {
            Vec3D min = new Vec3D(centerBottom.X - radius, centerBottom.Y - radius, centerBottom.Z);
            Vec3D max = new Vec3D(centerBottom.X + radius, centerBottom.Y + radius, centerBottom.Z + height);
            return new Box3D(min, max);
        }
    }
}