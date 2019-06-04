using Helion.World.Geometry;

namespace Helion.Render.OpenGL.Shared.Triangulator
{
    /// <summary>
    /// A helper class that will take some world and polygonize all the walls
    /// and floors so they can be rendered as triangles.
    /// </summary>
    public static class WorldTriangulator
    {
        public static SegmentTriangles Triangulate(Segment segment)
        {
            SegmentTriangles segmentTriangles = new SegmentTriangles(segment.Line, segment.Side);

            return segmentTriangles;
        }

        public static SubsectorTriangles Triangulate(Subsector subsector)
        {
            // TODO
            return new SubsectorTriangles();
        }
    }
}
