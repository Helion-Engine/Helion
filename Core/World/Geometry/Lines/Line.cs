using Helion.Maps.Specials;
using Helion.Util.Geometry;
using Helion.World.Entities;
using Helion.World.Geometry.Sides;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry.Lines
{
    public class Line
    {
        public readonly int Id;
        public readonly int MapId;
        public readonly Seg2D Segment;
        public readonly Side Front;
        public readonly Side? Back;
        public readonly Side[] Sides;
        public readonly SpecialArgs Args;
        public readonly LineFlags Flags;

        public bool OneSided => Back == null;
        public bool TwoSided => !OneSided;

        public Line(int id, int mapId, Seg2D segment, Side front, Side? back, LineFlags flags, SpecialArgs? args = null)
        {
            Precondition(id == mapId, "Line mismatch from generated ID to map ID");
            
            Id = id;
            MapId = mapId;
            Segment = segment;
            Front = front;
            Back = back;
            Sides = (back == null ? new[] { front } : new[] { front, back });
            Args = args ?? new SpecialArgs();
            Flags = flags;

            front.Line = this;
            if (back != null)
                back.Line = this;
        }

        /// <summary>
        /// If the line blocks the given entity. Only checks line properties
        /// and flags. No sector checking.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns>True if the entity is blocked by this line, false
        /// otherwise.</returns>
        public bool BlocksEntity(Entity entity)
        {
            return OneSided || (entity.Player != null && Flags.Blocking.Players);
        }
    }
}