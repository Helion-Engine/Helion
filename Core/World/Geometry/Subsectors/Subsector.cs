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
    public readonly LinkableList<Entity> Entities = new LinkableList<Entity>();

    private bool m_floorChanged;
    private bool m_ceilingChanged;

    public Subsector(int id, Sector sector, Box2D boundingBox, List<SubsectorSegment> clockwiseEdges)
    {
        Precondition(clockwiseEdges.Count >= 3, "Degenerate sector, must be at least a triangle");

        Id = id;
        Sector = sector;
        BoundingBox = boundingBox;
        ClockwiseEdges = clockwiseEdges;

        clockwiseEdges.ForEach(edge => edge.Subsector = this);
        sector.Floor.OnRenderingChanged += Floor_OnRenderingChanged;
        sector.Ceiling.OnRenderingChanged += Ceiling_OnRenderingChanged;
    }

    public bool CheckFloorRenderingChanged()
    {
        if (!m_floorChanged)
            return false;

        m_floorChanged = Sector.Floor.CheckRenderingChanged();
        return true;
    }

    public bool CheckCeilingRenderingChanged()
    {
        if (!m_ceilingChanged)
            return false;

        m_ceilingChanged = Sector.Ceiling.CheckRenderingChanged();
        return true;
    }

    private void Ceiling_OnRenderingChanged(object? sender, System.EventArgs e) => m_ceilingChanged = true;

    private void Floor_OnRenderingChanged(object? sender, System.EventArgs e) => m_floorChanged = true;

    public LinkableNode<Entity> Link(Entity entity)
    {
        Precondition(!Entities.Contains(entity), "Trying to link an entity to a sector twice");

        LinkableNode<Entity> node = DataCache.Instance.GetLinkableNodeEntity(entity);
        Entities.Add(node);
        return node;
    }
}

