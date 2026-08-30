using Helion.Geometry.Boxes;
using Helion.World.Geometry.Sectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry.Subsectors;

public class Subsector
{
    public int Id;
    public Sector Sector;
    public Box2D BoundingBox;    
    public int SegIndex;
    public int SegCount;
    public int IslandId;
    public int SectorIslandId;
    public bool Flood;
    // True if this subsector originally had less than 3 segments. All subsectors are fixed to have 3 for rendering.
    public bool Malformed;

    public Subsector(int id, Sector sector, Box2D boundingBox, int index, int count, bool malformed)
    {
        Precondition(ReferenceEquals(sector, Sector.Default) || count >= 3, "Degenerate sector, must be at least a triangle");

        Id = id;
        Sector = sector;
        BoundingBox = boundingBox;
        SegIndex = index;
        SegCount = count;
        Malformed = malformed;
    }

    public override string ToString() => $"Id={Id} SectorId={Sector.Id} Box={BoundingBox} Segs={SegCount}";
}
 