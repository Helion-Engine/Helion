using Helion.Maps.Geometry;
using Helion.Util.Geometry;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.World.Geometry
{
    public class Subsector
    {
        public readonly int Id;
        public Sector Sector;
        public List<Segment> ClockwiseEdges;
        public Box2Fixed BoundingBox;

        public Subsector(int id, Sector sector, List<Segment> clockwiseEdges, Box2Fixed boundingBox)
        {
            Precondition(clockwiseEdges.Count >= 3, "Degenerate sector, must be at least a triangle");

            Id = id;
            Sector = sector;
            ClockwiseEdges = clockwiseEdges;
            BoundingBox = boundingBox;
        }
    }
}
