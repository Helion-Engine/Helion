using Helion.Maps.Geometry;
using Helion.Util.Geometry;
using System.Collections.Generic;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry
{
    public class Subsector
    {
        public readonly int Id;
        public Sector Sector;
        public List<SubsectorEdge> ClockwiseEdges;
        public Box2D BoundingBox;

        public Subsector(int id, Sector sector, List<SubsectorEdge> clockwiseEdges, Box2D boundingBox)
        {
            Precondition(clockwiseEdges.Count >= 3, "Degenerate sector, must be at least a triangle");

            Id = id;
            Sector = sector;
            ClockwiseEdges = clockwiseEdges;
            BoundingBox = boundingBox;
        }
    }
}
