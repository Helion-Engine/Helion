using System.Collections.Generic;
using Helion.Util.Container.Linkable;
using Helion.Util.Extensions;
using Helion.World.Entities;
using Helion.World.Geometry.Sides;

namespace Helion.World.Geometry.Sectors
{
    public class Sector
    {
        public readonly int Id;
        public readonly int MapId;
        public readonly int Tag;
        public readonly List<Side> Sides = new List<Side>();
        public readonly SectorSpan DefaultSpan;
        public readonly List<SectorSpan> Spans = new List<SectorSpan>();
        public readonly LinkableList<Entity> Entities = new LinkableList<Entity>();
        public short LightLevel;

        public bool Has3DFloors => !Spans.Empty();

        public Sector(int id, int mapId, int tag, short lightLevel, SectorSpan defaultSpan)
        {
            Id = id;
            MapId = mapId;
            Tag = tag;
            LightLevel = lightLevel;
            DefaultSpan = defaultSpan;
            Spans.Add(defaultSpan);
        }
    }
}