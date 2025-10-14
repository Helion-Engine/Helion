using Helion.Geometry.Boxes;
using Helion.World.Geometry.Sectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry.Subsectors;

public class Subsector
{
    public readonly int Id;
    public Sector Sector;
    public readonly Box2D BoundingBox;    
    public readonly int SegIndex;
    public readonly int SegCount;
    public bool Flood;

    public Subsector(int id, Sector sector, Box2D boundingBox, int index, int count)
    {
        Precondition(ReferenceEquals(sector, Sector.Default) || count >= 3, "Degenerate sector, must be at least a triangle");

        Id = id;
        Sector = sector;
        BoundingBox = boundingBox;
        SegIndex = index;
        SegCount = count;
    }
}
 