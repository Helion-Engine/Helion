using System.Collections.Generic;
using Helion.Geometry.Boxes;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry.Subsectors;

public class Subsector
{
    public readonly int Id;
    public readonly Sector Sector;
    public readonly Box2D BoundingBox;
    public readonly List<SubsectorSegment> ClockwiseEdges;
    public readonly LinkableList<Entity> Entities = new();

    public Subsector(int id, Sector sector, Box2D boundingBox, List<SubsectorSegment> clockwiseEdges)
    {
        Precondition(clockwiseEdges.Count >= 3, "Degenerate sector, must be at least a triangle");

        Id = id;
        Sector = sector;
        BoundingBox = boundingBox;
        ClockwiseEdges = clockwiseEdges;
    }

    public LinkableNode<Entity> Link(Entity entity)
    {
        Precondition(!Entities.ContainsReference(entity), "Trying to link an entity to a sector twice");

        LinkableNode<Entity> node = entity.World.DataCache.GetLinkableNodeEntity(entity);
        Entities.Add(node);
        return node;
    }
}
