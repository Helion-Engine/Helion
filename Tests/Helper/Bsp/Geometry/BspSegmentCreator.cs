using Helion.Bsp.Geometry;
using Helion.Util.Geometry.Vectors;

namespace Helion.Test.Helper.Bsp.Geometry
{
    public static class BspSegmentCreator
    {
        /// <summary>
        /// A quick method for creating minisegs on the fly when you don't care
        /// about the raw positions but care about the indices.
        /// </summary>
        /// <remarks>
        /// Intended for BSP segment graph algorithms primarily. The collinear
        /// indices are all the same as well.
        /// </remarks>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <returns>A newly allocated segment. This is not part of any segment
        /// allocator.</returns>
        public static BspSegment Create(int startIndex, int endIndex)
        {
            BspVertex start = new BspVertex(new Vec2D(0, 1), startIndex);
            BspVertex end = new BspVertex(new Vec2D(1, 1), endIndex);
            return new BspSegment(start, end, 0);
        }
    }
}