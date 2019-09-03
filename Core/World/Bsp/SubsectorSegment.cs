using Helion.Util.Geometry;
using Helion.World.Geometry;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;

namespace Helion.World.Bsp
{
    /// <summary>
    /// An edge of a subsector.
    /// </summary>
    public class SubsectorSegment : Seg2D
    {
        public readonly int Id;
        public Side? Side;

        public Line? Line => Side?.Line;
        public bool IsMiniseg => Side == null;

        public SubsectorSegment(int id, Side? side, Vec2D start, Vec2D end) : base(start, end)
        {
            Id = id;
            Side = side;
        }
    }
}
