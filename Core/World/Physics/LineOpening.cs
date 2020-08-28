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

        public static bool IsOpen(Line line)
        {
            Assert.Precondition(line.Back != null, "Cannot create LineOpening with one sided line");

            Sector front = line.Front.Sector;
            Sector back = line.Back!.Sector;

            return Math.Min(front.Ceiling.Z, back.Ceiling.Z) - Math.Max(front.Floor.Z, back.Floor.Z) > 0;
        }

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

        public void SetTop(Entity other)
        {
            CeilingZ = other.LowestCeilingZ;
            FloorZ = other.Position.Z + other.Height;
            OpeningHeight = CeilingZ - FloorZ;
            DropOffZ = FloorZ;
        }

        public void SetBottom(Entity other)
        {
            CeilingZ = other.Position.Z;
            FloorZ = other.HighestFloorZ;
            OpeningHeight = CeilingZ - FloorZ;
            DropOffZ = FloorZ;
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

            if (entity.Box.Bottom < FloorZ)
                return CanStepUpInto(entity);
            
            return true;
        }
    }
}