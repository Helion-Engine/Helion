using System.Collections.Generic;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Geometry.Boxes;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry.Subsectors
{
    public class Subsector
    {
        public readonly int Id;
        public readonly Sector Sector;
        public readonly Box2D BoundingBox;
        public readonly List<SubsectorSegment> ClockwiseEdges;
        public readonly LinkableList<Entity> Entities = new LinkableList<Entity>();

        public Subsector(int id, Sector sector, Box2D boundingBox, List<SubsectorSegment> clockwiseEdges)
        {
            Precondition(clockwiseEdges.Count >= 3, "Degenerate sector, must be at least a triangle");

            Id = id;
            Sector = sector;
            BoundingBox = boundingBox;
            ClockwiseEdges = clockwiseEdges;
            
            clockwiseEdges.ForEach(edge => edge.Subsector = this);
        }
        
        public LinkableNode<Entity> Link(Entity entity)
        {
            Precondition(!Entities.Contains(entity), "Trying to link an entity to a sector twice");

            LinkableNode<Entity> node = DataCache.Instance.GetLinkableNodeEntity(entity);
            Entities.Add(node);
            return node;
        }
    }
}