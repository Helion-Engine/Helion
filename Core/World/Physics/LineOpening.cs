using System;
using Helion.Maps.Geometry;
using Helion.Util.Geometry;
using Helion.World.Entities;

namespace Helion.World.Physics
{
    /// <summary>
    /// Represents the opening space for a line segment.
    /// </summary>
    public struct LineOpening
    {
        /// <summary>
        /// The top of the opening. This is not guaranteed to be above the
        /// ceiling value.
        /// </summary>
        public readonly double CeilingZ;
        
        /// <summary>
        /// The bottom of the opening. This is not guaranteed to be below the
        /// ceiling value.
        /// </summary>
        public readonly double FloorZ;

        /// <summary>
        /// How tall the opening is along the Z axis.
        /// </summary>
        public readonly double OpeningHeight;

        public LineOpening(Sector front, Sector back)
        {
            CeilingZ = Math.Min(front.Ceiling.Z, back.Ceiling.Z);
            FloorZ = Math.Max(front.Floor.Z, back.Floor.Z);
            OpeningHeight = CeilingZ - FloorZ;
        }
        
        public bool CanStepUpInto(Entity entity)
        {
            return entity.Box.Bottom < FloorZ && entity.Box.Bottom >= FloorZ - entity.Definition.Properties.StepHeight;
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