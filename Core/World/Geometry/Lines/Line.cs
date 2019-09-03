using Helion.Maps.Specials;
using Helion.Util.Geometry;
using Helion.World.Entities;
using Helion.World.Geometry.Sides;

namespace Helion.World.Geometry.Lines
{
    public class Line
    {
        public readonly int Id;
        public readonly Seg2D Segment;
        public readonly Side Front;
        public readonly Side? Back;
        public readonly Side[] Sides;
        public readonly SpecialArgs Args;
        public LineFlags Flags;

        public bool OneSided => Back == null;
        public bool TwoSided => !OneSided;
        
        /// <summary>
        /// If the line blocks the given entity. Only checks line properties
        /// and flags. No sector checking.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns>True if the entity is blocked by this line, false
        /// otherwise.</returns>
        public bool BlocksEntity(Entity entity)
        {
            if (OneSided)
                return true;

            return entity.Player != null && Flags.Blocking.Players;
        }
    }
}