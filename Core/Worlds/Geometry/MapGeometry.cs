using System.Collections.Generic;
using System.Linq;
using Helion.Worlds.Bsp;
using Helion.Worlds.Geometry.Builder;
using Helion.Worlds.Geometry.Lines;
using Helion.Worlds.Geometry.Sectors;
using Helion.Worlds.Geometry.Walls;

namespace Helion.Worlds.Geometry
{
    public class MapGeometry
    {
        public readonly List<Line> Lines;
        public readonly List<Side> Sides;
        public readonly List<Wall> Walls;
        public readonly List<Sector> Sectors;
        public readonly List<SectorPlane> SectorPlanes;
        public readonly BspTree BspTree;
        private readonly Dictionary<int, List<Sector>> m_tagToSector = new();

        internal MapGeometry(GeometryBuilder builder)
        {
            Lines = builder.Lines;
            Sides = builder.Sides;
            Walls = builder.Walls;
            Sectors = builder.Sectors;
            SectorPlanes = builder.SectorPlanes;
            BspTree = builder.BspTree;

            TrackSectorsByTag();
        }

        public IEnumerable<Sector> FindBySectorTag(int tag)
        {
            return m_tagToSector.TryGetValue(tag, out List<Sector>? sectors) ? sectors : Enumerable.Empty<Sector>();
        }

        private void TrackSectorsByTag()
        {
            foreach (Sector sector in Sectors.Where(s => s.Tag != Sector.NoTag))
            {
                if (m_tagToSector.TryGetValue(sector.Tag, out List<Sector>? sectors))
                    sectors.Add(sector);
                else
                    m_tagToSector[sector.Tag] = new List<Sector> { sector };
            }
        }
    }
}