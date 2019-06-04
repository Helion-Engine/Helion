using Helion.Maps.Geometry;
using Helion.Util.Geometry;

namespace Helion.World.Geometry
{
    /// <summary>
    /// An edge of a subsector.
    /// </summary>
    public class Segment : Seg2Fixed
    {
        public readonly int Id;
        public Side? Side;
        public readonly int LineXOffset = 0;

        public Line? Line => Side?.Line;
        public bool IsMiniseg => Side == null;

        public Segment(int id, Side? side, Vec2Fixed start, Vec2Fixed end) : base(start, end)
        {
            Id = id;
            Side = side;

            if (side != null)
                LineXOffset = CalculateLineXOffset();
        }

        private int CalculateLineXOffset()
        {
            // TODO
            return 0;
        }
    }
}
