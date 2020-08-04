using System;
using Helion.Util.Assertion;
using Helion.Util.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Physics
{
    /// <summary>
    /// Represents the opening space for a line segment.
    /// </summary>
    public class LineOpening
    {
        /// <summary>
        /// The top of the opening. This is not guaranteed to be above the
        /// ceiling value.
        /// </summary>
        public double CeilingZ;
        
        /// <summary>
        /// The bottom of the opening. This is not guaranteed to be below the
        /// ceiling value.
        /// </summary>
        public double FloorZ;

        /// <summary>
        /// How tall the opening is along the Z axis.
        /// </summary>
        public double OpeningHeight;

        public double DropOffZ;

        public LineOpening()
        {
            CeilingZ = 0;
            FloorZ = 0;
            DropOffZ = 0;
            OpeningHeight = 0;
        }

        public LineOpening(in Vec2D position, Line line)
        {
            Set(position, line);
        }

        public void Set(in Vec2D position, Line line)
        {
            Assert.Precondition(line.Back != null, "Cannot create LineOpening with one sided line");

            Sector front = line.Front.Sector;
            Sector back = line.Back!.Sector;
            CeilingZ = Math.Min(front.Ceiling.Z, back.Ceiling.Z);
            FloorZ = Math.Max(front.Floor.Z, back.Floor.Z);
            OpeningHeight = CeilingZ - FloorZ;

            if (line.Segment.OnRight(position))
                DropOffZ = back.Floor.Z;
            else
                DropOffZ = front.Floor.Z;
        }
        
        public bool CanStepUpInto(Entity entity)
        {
            return entity.Box.Bottom < FloorZ && entity.Box.Bottom >= FloorZ - entity.GetMaxStepHeight();
        }

        public bool Fits(Entity entity) => entity.Height <= OpeningHeight;

        public bool CanPassOrStepThrough(Entity entity)
        {
            if (!Fits(entity) || entity.Box.Top > CeilingZ)
                return false;

            // Block monsters and things with no dropoff if the drop is higher than the step height
            if (!entity.Flags.Float && entity.IsEnemyMove && (entity.Flags.Monster || entity.Flags.NoDropoff) && 
                Math.Abs(entity.Position.Z - DropOffZ) > entity.GetMaxStepHeight())
            {
                return false;
            }

            if (entity.Box.Bottom < FloorZ)
                return CanStepUpInto(entity);
            
            return true;
        }
    }
}