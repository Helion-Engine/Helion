using Helion.Geometry.Vectors;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using System.Collections.Generic;
using System.Linq;

namespace Helion.World.Geometry.Islands;

/// <summary>
/// A helper class that classifies subsectors into islands.
/// </summary>
public static class IslandClassifier
{
    static int IslandId = 0;
    static int SectorIslandId = 0;
    static HashSet<BspSubsector> ProcessedSubsectors = new();

    public static List<Island> Classify(List<BspSubsector> subsectors, List<Sector> sectors, List<Line> lines, int sectorId = -1)
    {
        List<Island> islands = new();
        IEnumerable<BspSubsector> traverseSubsectors = sectorId == -1 ? subsectors : subsectors.Where(x => x.SectorId == sectorId);

        foreach (BspSubsector subsector in traverseSubsectors)
        {
            if (ProcessedSubsectors.Contains(subsector)) 
                continue;

            Island island = new(sectorId == -1 ? IslandId++ : SectorIslandId);
            islands.Add(island);
            TraverseSubsectors(subsector, island, ProcessedSubsectors, sectors, lines, sectorId);
        }

        foreach (var island in islands)
        {
            Vec2D min = new(double.MaxValue, double.MaxValue);
            Vec2D max = new(double.MinValue, double.MinValue);
            foreach (var subsector in island.Subsectors)
            {
                if (subsector.Box.Min.X < min.X)
                    min.X = subsector.Box.Min.X;
                if (subsector.Box.Min.Y < min.Y)
                    min.Y = subsector.Box.Min.Y;
                if (subsector.Box.Max.X > max.X)
                    max.X = subsector.Box.Max.X;
                if (subsector.Box.Max.Y > max.Y)
                    max.Y = subsector.Box.Max.Y;
            }
            island.Box = new(min, max);
        }

        ProcessedSubsectors.Clear();
        return islands;
    }

    private static void TraverseSubsectors(BspSubsector initialSubsector, Island island, HashSet<BspSubsector> processedSubsectors, 
        List<Sector> sectors, List<Line> lines, int sectorId)
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
            if (sectorId == -1 && subsector.SectorId.HasValue)
                sectors[subsector.SectorId.Value].Island = island;

            if (sectorId == -1)
                subsector.IslandId = island.Id;
            else
                subsector.SectorIslandId = island.Id;

            foreach (BspSubsectorSeg seg in subsector.Segments)
            {
                if (seg.LineId != null && !visitedLines.Contains(seg.LineId.Value))
                {
                    island.LineIds.Add(seg.LineId.Value);
                    visitedLines.Add(seg.LineId.Value);
                }

                if (seg.Partner != null && !processedSubsectors.Contains(seg.Partner.Subsector))
                {
                    if (sectorId == -1 || sectorId == seg.Partner.Subsector.SectorId)
                        subsectorsToVisit.Push(seg.Partner.Subsector);
                }
            }
        }
    }
}
