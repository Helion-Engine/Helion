using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Util.Geometry;

namespace Helion.World.Bsp
{
    /// <summary>
    /// An edge of a subsector.
    /// </summary>
    public class SubsectorEdge : Seg2D
    {
        public readonly int Id;
        public Side? Side;

        public Line? Line => Side?.Line;
        public bool IsMiniseg => Side == null;

        public SubsectorEdge(int id, Side? side, Vec2D start, Vec2D end) : base(start, end)
        {
            Id = id;
            Side = side;
        }
    }
}
