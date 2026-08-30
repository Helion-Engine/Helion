using Helion.World.Bsp;
using Helion.World.Geometry.Builder;
using Helion.World.Geometry.Islands;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using System;
using System.Collections.Generic;
using System.Linq;

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
    private int m_nextLineId;
    private int m_nextSideId;
    private int m_nextSectorId;

    internal MapGeometry(GeometryBuilder builder, CompactBspTree bspTree)
    {
        Lines = builder.Lines;
        Sides = builder.Sides;
        Sectors = builder.Sectors;
        CompactBspTree = bspTree;

        TrackSectorsByTag();
        TrackLinesByLineId();

        m_nextLineId = Lines.Count;
        m_nextSideId = Sides.Count;
        m_nextSectorId = Sectors.Count;
    }
    public int CreateNewLineId() => m_nextLineId++;
    public int CreateNewSideId() => m_nextSideId++;
    public int CreateNewSectorId() => m_nextSectorId++;

    public int GetLineCount() => m_nextLineId;
    public int GetSideCount() => m_nextSideId;
    public int GetSectorCount() => m_nextSectorId;

    public void ClassifyIslands()
    {
        var islandClassifier = new IslandClassifier(CompactBspTree);
        IslandGeometry.Islands = islandClassifier.Classify(CompactBspTree.Subsectors, Sectors, Lines.Count);
        IslandGeometry.SectorIslands = islandClassifier.ClassifySectors(Sectors, Lines.Count);

        SubsectorToIslandId = new int[CompactBspTree.Subsectors.Length];
        foreach (var subsector in CompactBspTree.Subsectors)
            SubsectorToIslandId[subsector.Id] = subsector.IslandId;

        for (int sectorId = 0; sectorId < IslandGeometry.SectorIslands.Length; sectorId++)
        {
            foreach (var island in IslandGeometry.SectorIslands[sectorId])
            {
                bool islandFlooded = false;
                foreach (var subsector in island.Subsectors)
                {
                    island.ParentIsland = IslandGeometry.Islands[subsector.IslandId];
                    if (subsector.SegCount >= 3)
                        continue;

                    IslandGeometry.BadSubsectors.Add(subsector.Id);
                    IslandGeometry.FloodSectors.Add(subsector.Sector.Id);

                    if (islandFlooded)
                        continue;
                    
                    SetContainingSectorsToFlood(CompactBspTree, subsector);
                    islandFlooded = true;
                }
            }
        }
    }

    private void SetContainingSectorsToFlood(CompactBspTree bspTree, Subsector subsector)
    {
        // This could work by sector island instead of the entire sector but it's unlikely to matter and the renderer will need to be aware of this.
        var smallestFloodPerimeter = double.MaxValue;
        var smallestFloodSector = -1;
        var noArea = subsector.BoundingBox.Min.X == subsector.BoundingBox.Max.X || subsector.BoundingBox.Min.Y == subsector.BoundingBox.Max.Y;

        for (int sectorId = 0; sectorId < IslandGeometry.SectorIslands.Length; sectorId++)
        {
            var islands = IslandGeometry.SectorIslands[sectorId];
            if (islands.Count == 0)
                continue;
                        
            // Set all adjacent sectors to flood. This deals with cases like BTSX MAP02 exit line 2560.
            if (noArea)
            {
                for (int i = 0; i < subsector.SegCount; i++)
                //foreach (var seg in subsector.Segments)
                {
                    ref var seg = ref bspTree.Segments[subsector.SegIndex + i];
                    IslandGeometry.FloodSectors.Add(seg.FrontSectorId);
                }
            }

            foreach (var island in islands)
            {
                if (island.Flood)
                    continue;

                if (!island.ContainsInclusive(subsector.BoundingBox))
                    continue;

                if (sectorId == subsector.Sector.Id)
                {
                    SetIslandFlooded(island);
                    continue;
                }

                if (noArea)
                {
                    // If the subsector has no area then treat as a line with two vertices. Likely a single self referencing sector line.
                    if (!island.LineInsideSector(subsector.BoundingBox.Min, subsector.BoundingBox.Max))
                        continue;

                    var perimeter = (island.Box.Width + island.Box.Height) * 2;
                    if (perimeter < smallestFloodPerimeter)
                    {
                        smallestFloodPerimeter = perimeter;
                        smallestFloodSector = sectorId;
                    }
                }
                else
                {
                    var perimeter = (island.Box.Width + island.Box.Height) * 2;
                    if (perimeter >= smallestFloodPerimeter && !island.BoxInsideSector(subsector.BoundingBox))
                        continue;

                    if (perimeter < smallestFloodPerimeter)
                    {
                        smallestFloodPerimeter = perimeter;
                        smallestFloodSector = sectorId;
                    }
                }
            }
        }

        if (smallestFloodSector != -1)
            IslandGeometry.FloodSectors.Add(smallestFloodSector);
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
        TrackLineId(line, lineId);
    }

    private void TrackSectorsByTag()
    {
        foreach (var sector in Sectors)
        {
            TrackSectorTag(sector, sector.Tag);

            foreach (var tag in sector.MoreTags)
                TrackSectorTag(sector, tag);
        }
    }

    private void TrackSectorTag(Sector sector, int tag)
    {
        if (m_tagToSector.TryGetValue(tag, out var sectors))
            sectors.Add(sector);
        else
            m_tagToSector[tag] = [sector];
    }

    private void TrackLinesByLineId()
    {
        foreach (var line in Lines)
        {
            TrackLineId(line, line.MapLineId);

            foreach (var id in line.MoreLineIds)
                TrackLineId(line, id);
        }
    }

    private void TrackLineId(Line line, int mapLineId)
    {
        if (line.MapLineId == Line.NoLineId)
            return;

        if (m_idToLine.TryGetValue(mapLineId, out var lines))
            lines.Add(line);
        else
            m_idToLine[mapLineId] = [line];
    }
}
