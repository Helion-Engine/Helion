using Helion.Maps.Geometry;
using Helion.World.Geometry;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.Render.Shared.Triangulator
{
    /// <summary>
    /// A fan of triangles that make up the floor and ceiling polygon for a
    /// subsector.
    /// </summary>
    public class SubsectorTriangles
    {
        /// <summary>
        /// The subsector that made these triangles.
        /// </summary>
        public readonly Subsector Subsector;

        /// <summary>
        /// A counter-clockwise fan of triangles that make up the floor, with
        /// the assumption that the camera is above it on the Z axis.
        /// </summary>
        public List<Triangle> Floor;

        /// <summary>
        /// A clockwise fan of triangles that make up the ceiling, with the
        /// assumption that the camera is underneath it on the Z axis.
        /// </summary>
        public List<Triangle> Ceiling;

        /// <summary>
        /// The sector for the subsector.
        /// </summary>
        public Sector Sector => Subsector.Sector;

        public SubsectorTriangles(Subsector subsector, List<Triangle> floor, List<Triangle> ceiling)
        {
            Precondition(floor.Count == subsector.ClockwiseEdges.Count - 2, "Did not make a proper fan from subsector edges");
            Precondition(floor.Count == ceiling.Count, "Cannot have different triangle counts for floor/ceiling");

            Subsector = subsector;
            Floor = floor;
            Ceiling = ceiling;
        }
    }
}
