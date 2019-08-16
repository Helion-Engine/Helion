using System.Collections.Generic;
using Helion.Maps.Geometry;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Bsp
{
    public class Subsector
    {
        public readonly int Id;
        public readonly Sector Sector;
        public readonly List<SubsectorEdge> ClockwiseEdges;
        public readonly Box2D BoundingBox;

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
