using Helion.Geometry.Vectors;
using Helion.Util.Assertion;
using Helion.World.Bsp;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using System.Collections.Generic;
using System.Linq;

namespace Helion.World.Geometry.Islands;

/// <summary>
/// A helper class that classifies subsectors into islands.
/// </summary>
public class IslandClassifier
{
    int IslandId;
    int SectorIslandId;
    int SubsectorCounter = 1;
    int LineCounter = 1;
    int[] ProcessedSubsectors = [];
    int[] VisitedLines = [];

    private readonly CompactBspTree m_bspTree;
    private readonly Subsector[] m_subsectorById;

    public IslandClassifier(CompactBspTree bspTree)
    {
        m_bspTree = bspTree;
        m_subsectorById = new Subsector[bspTree.Subsectors.Length];
        for (int i = 0; i < bspTree.Subsectors.Length; i++)
        {
            var subsector = bspTree.Subsectors[i];
            m_subsectorById[subsector.Id] = subsector;
        }
    }

    public List<Island>[] ClassifySectors(List<Sector> sectors, int lineCount)
    {
        List<Island>[] islands = new List<Island>[sectors.Count];
        var subsectorLookup = m_bspTree.Subsectors.GroupBy(x => x.Sector.Id).ToDictionary(x => x.Key, x => x.ToArray());

        for (int sectorId = 0; sectorId < islands.Length; sectorId++)
        {
            if (!subsectorLookup.TryGetValue(sectorId, out var sectorSubsectors))
            {
                islands[sectorId] = [];
                continue;
            }

            islands[sectorId] = Classify(sectorSubsectors, sectors, lineCount, sectorId);
        }

        return islands;
    }

    public List<Island> Classify(Subsector[] subsectors, List<Sector> sectors, int lineCount, int sectorId = -1)
    {
        IslandId = 0;
        SectorIslandId = 0;
        List<Island> islands = [];
        SubsectorCounter++;

        if (ProcessedSubsectors.Length < subsectors.Length)
            ProcessedSubsectors = new int[subsectors.Length];

        if (VisitedLines.Length < lineCount)
            VisitedLines = new int[lineCount];

        foreach (var subsector in subsectors)
        {
            if (ProcessedSubsectors[subsector.Id] == SubsectorCounter) 
                continue;

            Island island = new(sectorId == -1 ? IslandId++ : SectorIslandId++);
            islands.Add(island);
            if (sectorId != -1)
                island.SectorId = sectorId;
            TraverseSubsectors(subsector, island, sectors, sectorId);
        }

        foreach (var island in islands)
        {
            Vec2D min = new(double.MaxValue, double.MaxValue);
            Vec2D max = new(double.MinValue, double.MinValue);
            foreach (var subsector in island.Subsectors)
            {
                if (subsector.BoundingBox.Min.X < min.X)
                    min.X = subsector.BoundingBox.Min.X;
                if (subsector.BoundingBox.Min.Y < min.Y)
                    min.Y = subsector.BoundingBox.Min.Y;
                if (subsector.BoundingBox.Max.X > max.X)
                    max.X = subsector.BoundingBox.Max.X;
                if (subsector.BoundingBox.Max.Y > max.Y)
                    max.Y = subsector.BoundingBox.Max.Y;
            }
            island.Box = new(min, max);
        }

        return islands;
    }

    private void TraverseSubsectors(Subsector initialSubsector, Island island, 
        List<Sector> sectors, int sectorId)
    {
        LineCounter++;
        Stack<Subsector> subsectorsToVisit = new();
        subsectorsToVisit.Push(initialSubsector);

        while (subsectorsToVisit.Count > 0)
        {
            var subsector = subsectorsToVisit.Pop();

            if (ProcessedSubsectors[subsector.Id] == SubsectorCounter)
                continue;

            ProcessedSubsectors[subsector.Id] = SubsectorCounter;
            island.Subsectors.Add(subsector);
            if (sectorId == -1)
            {
                var sector = sectors[subsector.Sector.Id];
                sector.Island = island;
            }

            if (sectorId == -1)
                subsector.IslandId = island.Id;
            else
                subsector.SectorIslandId = island.Id;

            for (int i = 0; i < subsector.SegCount; i++)
            {
                ref var seg = ref m_bspTree.Segments[subsector.SegIndex + i];
                if (seg.LineId != -1 && VisitedLines[seg.LineId] != LineCounter)
                {
                    island.LineIds.Add(seg.LineId);
                    VisitedLines[seg.LineId] = LineCounter;
                }

                var subsectorId = m_bspTree.PartnerSegmentSubsectors[subsector.SegIndex + i];
                if (subsectorId == -1)
                    continue;

                if (ProcessedSubsectors[subsectorId] != SubsectorCounter)
                {
                    // The root is the last subsector so the array is in reverse id order.
                    var partnerSubsector = m_subsectorById[subsectorId];
                    Assert.Precondition(partnerSubsector.Id == subsectorId, "Incorrect subsector");
                    if (sectorId == -1 || sectorId == partnerSubsector.Sector.Id)
                        subsectorsToVisit.Push(partnerSubsector);
                }
            }
        }
    }
}
