using Helion.Maps.Geometry;
using Helion.World.Geometry;

namespace Helion.Render.Shared.Triangulator
{
    /// <summary>
    /// A collection of wall triangles for a subsector segment.
    /// </summary>
    public class SegmentTriangles
    {
        /// <summary>
        /// The line that the segment belongs to.
        /// </summary>
        public readonly Line Line;

        /// <summary>
        /// The side the segment belongs to.
        /// </summary>
        public readonly Side Side;

        /// <summary>
        /// The segment that this component represents.
        /// </summary>
        public readonly Segment Segment;

        /// <summary>
        /// The upper wall triangles, if any. Absence implies a one sided line.
        /// </summary>
        public WallTriangles? Upper;

        /// <summary>
        /// The triangles for the middle component.
        /// </summary>
        public WallTriangles Middle;

        /// <summary>
        /// The lower wall triangles, if any. Absence implies a one sided line.
        /// </summary>
        public WallTriangles? Lower;

        public SegmentTriangles(Segment segment, Line line, Side side, WallTriangles? upper, 
            WallTriangles middle, WallTriangles? lower)
        {
            Line = line;
            Side = side;
            Segment = segment;
            Upper = upper;
            Middle = middle;
            Lower = lower;
        }
    }

    /// <summary>
    /// The quad in counter clockwise triangle form that makes up a component
    /// of a wall.
    /// </summary>
    public class WallTriangles
    {
        /// <summary>
        /// The plane that defines the bottom of the wall.
        /// </summary>
        public readonly SectorFlat FloorPlane;

        /// <summary>
        /// The plane that defines the top of the wall.
        /// </summary>
        public readonly SectorFlat CeilingPlane;

        /// <summary>
        /// The upper triangle, whereby vertex index 1 -> 2 is shared with the
        /// lower triangle.
        /// </summary>
        public Triangle UpperTriangle;

        /// <summary>
        /// The lower triangle, whereby vertex index 0 -> 1 is shared with the
        /// upper triangle.
        /// </summary>
        public Triangle LowerTriangle;

        public WallTriangles(SectorFlat floorPlane, SectorFlat ceilingPlane, Triangle upperTriangle, 
            Triangle lowerTriangle)
        {
            CeilingPlane = ceilingPlane;
            FloorPlane = floorPlane;
            UpperTriangle = upperTriangle;
            LowerTriangle = lowerTriangle;
        }
    }
}
