using Helion.Maps.Geometry;

namespace Helion.Render.OpenGL.Shared.Triangulator
{
    /// <summary>
    /// A collection of wall triangles for a subsector segment.
    /// </summary>
    public class SegmentTriangles
    {
        public readonly Line? Line;
        public readonly Side? Side;
        public WallTriangles? Upper;
        public WallTriangles? Middle;
        public WallTriangles? Lower;

        public SegmentTriangles(Line? line, Side? side)
        {
            Line = line;
            Side = side;
        }
    }

    /// <summary>
    /// The quad in counter clockwise triangle form that makes up a component
    /// of a wall.
    /// </summary>
    public class WallTriangles
    {
        /// <summary>
        /// The plane that defines the top of the wall.
        /// </summary>
        public readonly SectorFlat CeilingPlane;

        /// <summary>
        /// The plane that defines the bottom of the wall.
        /// </summary>
        public readonly SectorFlat FloorPlane;

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

        public WallTriangles(SectorFlat ceilingPlane, SectorFlat floorPlane, Triangle upperTriangle, 
            Triangle lowerTriangle)
        {
            CeilingPlane = ceilingPlane;
            FloorPlane = floorPlane;
            UpperTriangle = upperTriangle;
            LowerTriangle = lowerTriangle;
        }
    }
}
