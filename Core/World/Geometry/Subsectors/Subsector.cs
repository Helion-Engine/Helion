using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.World.Geometry.Sectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry.Subsectors;

public class Subsector
{
    public static readonly Subsector Default = new(0, Sector.Default, (Vec2D.Zero, Vec2D.Zero), 0, 0);

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
 