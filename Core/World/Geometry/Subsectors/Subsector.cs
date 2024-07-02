using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Util.Container;
using Helion.World.Entities;
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
    public readonly LinkableList<Entity> Entities = new();
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

    public LinkableNode<Entity> Link(Entity entity)
    {
        Precondition(!Entities.ContainsReference(entity), "Trying to link an entity to a sector twice");

        LinkableNode<Entity> node = WorldStatic.DataCache.GetLinkableNodeEntity(entity);
        Entities.Add(node);
        return node;
    }
}
