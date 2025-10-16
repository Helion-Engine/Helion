using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry.Vectors;
using Helion.Maps;
using Helion.Util.Container;
using Helion.World.Bsp;
using Helion.World.Geometry.Builder;
using Helion.World.Geometry.Islands;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;

namespace Helion.World.Geometry;

public struct IslandGeometry
{
    public IslandGeometry()
    {
        BadSubsectors = [];
        FloodSectors = [];
        Islands = [];
        SectorIslands = [];
    }

    public HashSet<int> BadSubsectors;
    public HashSet<int> FloodSectors;
    public List<Island> Islands;
    public List<Island>[] SectorIslands;
}

public class MapGeometry
{
    public readonly List<Line> Lines;
    public readonly List<Side> Sides;
    public readonly List<Sector> Sectors;
    public readonly CompactBspTree CompactBspTree;
    public int[] SubsectorToIslandId = [];

    public IslandGeometry IslandGeometry = new();

    private readonly Dictionary<int, IList<Sector>> m_tagToSector = [];
    private readonly Dictionary<int, IList<Line>> m_idToLine = [];
    private BspTreeNew? m_bspTree;

    public BspTreeNew? GetBspTree() => m_bspTree;
    public void ClearBspTree()
    {
        if (m_bspTree == null)
            return;

        m_bspTree.Nodes = null!;
        m_bspTree.Segments = null!;
        foreach (var subsector in m_bspTree.Subsectors)
            subsector.Segments = null!;
        m_bspTree.Subsectors = null!;
        m_bspTree = null;
    }

    internal MapGeometry(GeometryBuilder builder, CompactBspTree bspTree, BspTreeNew bspTreeNew)
    {
        Lines = builder.Lines;
        Sides = builder.Sides;
        Sectors = builder.Sectors;
        CompactBspTree = bspTree;
        m_bspTree = bspTreeNew;

        TrackSectorsByTag();
        TrackLinesByLineId();
    }

    public void ClassifyIslands()
    {
        if (m_bspTree == null)
            return;

        var islandClassifier = new IslandClassifier();
        IslandGeometry.Islands = islandClassifier.Classify(m_bspTree.Subsectors, Sectors, Lines.Count);
        IslandGeometry.SectorIslands = islandClassifier.ClassifySectors(m_bspTree.Subsectors, Sectors, Lines.Count);

        SubsectorToIslandId = new int[m_bspTree.Subsectors.Count];
        foreach (var subsector in m_bspTree.Subsectors)
            SubsectorToIslandId[subsector.Id] = subsector.IslandId;

        for (int sectorId = 0; sectorId < IslandGeometry.SectorIslands.Length; sectorId++)
        {
            foreach (var island in IslandGeometry.SectorIslands[sectorId])
            {
                bool islandFlooded = false;
                foreach (var subsector in island.Subsectors)
                {
                    island.ParentIsland = IslandGeometry.Islands[subsector.IslandId];
                    if (subsector.Segments.Count >= 3)
                        continue;

                    IslandGeometry.BadSubsectors.Add(subsector.Id);
                    if (subsector.SectorId.HasValue)
                        IslandGeometry.FloodSectors.Add(subsector.SectorId.Value);

                    if (islandFlooded)
                        continue;
                    
                    SetContainingSectorsToFlood(subsector);
                    islandFlooded = true;
                }
            }
        }
    }

    private void SetContainingSectorsToFlood(BspSubsector subsector)
    {
        // This could work by sector island instead of the entire sector but it's unlikely to matter and the renderer will need to be aware of this.
        double? smallestFloodPerimeter = null;
        int? smallestFloodSector = null;
        var noArea = subsector.Box.Min.X == subsector.Box.Max.X || subsector.Box.Min.Y == subsector.Box.Max.Y;

        for (int sectorId = 0; sectorId < IslandGeometry.SectorIslands.Length; sectorId++)
        {
            var islands = IslandGeometry.SectorIslands[sectorId];
            if (islands.Count == 0)
                continue;

            foreach (var island in islands)
            {
                if (island.Flood)
                    continue;

                if (!island.ContainsInclusive(subsector.Box))
                    continue;

                if (sectorId == subsector.SectorId)
                    SetIslandFlooded(island);

                // If the subsector has no area then treat as a line with two vertices. Likely a single self referencing sector line.
                // This deals with cases like BTSX MAP02 exit line 2560.
                if (noArea)
                {
                    var v1 = subsector.Box.Min;
                    var v2 = subsector.Box.Max;
                    if (sectorId == subsector.SectorId)
                        continue;

                    if (!island.LineInsideSector(v1, v2))
                        continue;

                    IslandGeometry.FloodSectors.Add(sectorId);
                }
                else
                {
                    if (sectorId == subsector.SectorId)
                        continue;

                    if (!island.BoxInsideSector(subsector.Box))
                        continue;

                    var perimeter = (island.Box.Width + island.Box.Height) * 2;
                    if (smallestFloodPerimeter == null || perimeter < smallestFloodPerimeter)
                    {
                        smallestFloodPerimeter = perimeter;
                        smallestFloodSector = sectorId;
                    }
                }
            }
        }

        if (smallestFloodSector != null)
            IslandGeometry.FloodSectors.Add(smallestFloodSector.Value);
    }

    private void SetIslandFlooded(Island floodedIsland)
    {
        floodedIsland.Flood = true;
        for (int i = 0; i < floodedIsland.Subsectors.Count; i++)
            IslandGeometry.BadSubsectors.Add(floodedIsland.Subsectors[i].Id);
    }

    public IList<Sector> FindBySectorTag(int tag)
    {
        return m_tagToSector.TryGetValue(tag, out IList<Sector>? sectors) ? sectors : Array.Empty<Sector>();
    }

    public IEnumerable<Line> FindByLineId(int lineId)
    {
        return m_idToLine.TryGetValue(lineId, out IList<Line>? lines) ? lines : Enumerable.Empty<Line>();
    }

    public void SetLineId(Line line, int lineId)
    {
        line.MapLineId = lineId;
        TrackLineId(line);
    }

    private void TrackSectorsByTag()
    {
        foreach (Sector sector in Sectors)
        {
            if (m_tagToSector.TryGetValue(sector.Tag, out IList<Sector>? sectors))
                sectors.Add(sector);
            else
                m_tagToSector[sector.Tag] = new List<Sector> { sector };
        }
    }

    private void TrackLinesByLineId()
    {
        foreach (Line line in Lines)
        {
            if (line.MapLineId == Line.NoLineId)
                continue;

            TrackLineId(line);
        }
    }

    private void TrackLineId(Line line)
    {
        if (m_idToLine.TryGetValue(line.MapLineId, out IList<Line>? lines))
            lines.Add(line);
        else
            m_idToLine[line.MapLineId] = [line];
    }
}
