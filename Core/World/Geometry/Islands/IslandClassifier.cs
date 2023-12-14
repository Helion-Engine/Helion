using Helion.Maps;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.World.Geometry.Islands;

/// <summary>
/// A helper class that classifies subsectors into islands.
/// </summary>
public static class IslandClassifier
{
    public static List<Island> Classify(List<BspSubsector> subsectors, List<Sector> sectors, List<Line> lines)
    {
        int islandId = 0;
        List<Island> islands = new();
        HashSet<BspSubsector> processedSubsectors = new();

        foreach (BspSubsector subsector in subsectors)
        {
            if (processedSubsectors.Contains(subsector)) 
                continue;

            Island island = new(islandId++);
            TraverseSubsectors(subsector, island, processedSubsectors, sectors, lines);
            islands.Add(island);
        }

        return islands;
    }

    private static void TraverseSubsectors(BspSubsector initialSubsector, Island island, HashSet<BspSubsector> processedSubsectors, 
        List<Sector> sectors, List<Line> lines)
    {
        HashSet<int> visitedLines = new();
        Stack<BspSubsector> subsectorsToVisit = new();
        subsectorsToVisit.Push(initialSubsector);

        while (subsectorsToVisit.Count > 0)
        {
            BspSubsector subsector = subsectorsToVisit.Pop();

            if (processedSubsectors.Contains(subsector))
                continue;

            processedSubsectors.Add(subsector);
            island.Subsectors.Add(subsector);
            if (subsector.SectorId.HasValue)
                sectors[subsector.SectorId.Value].Island = island;

            foreach (BspSubsectorSeg seg in subsector.Segments)
            {
                if (seg.LineId != null && !visitedLines.Contains(seg.LineId.Value))
                {
                    island.LineIds.Add(seg.LineId.Value);
                    visitedLines.Add(seg.LineId.Value);
                }

                if (seg.Partner != null && !processedSubsectors.Contains(seg.Partner.Subsector))
                    subsectorsToVisit.Push(seg.Partner.Subsector);
            }
        }
    }
}
