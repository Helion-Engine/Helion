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

    public static List<Island>[] ClassifySectors(List<BspSubsector> subsectors, List<Sector> sectors)
    {
        List<Island>[] islands = new List<Island>[sectors.Count];
        var subsectorLookup = subsectors.Where(x => x.SectorId.HasValue).GroupBy(x => x.SectorId.Value).ToDictionary(x => x.Key, x => x.ToList());

        for (int sectorId = 0; sectorId < islands.Length; sectorId++)
        {
            if (!subsectorLookup.TryGetValue(sectorId, out var sectorSubsectors))
            {
                islands[sectorId] = new List<Island>();
                continue;
            }

            islands[sectorId] = Classify(sectorSubsectors, sectors, sectorId);
        }

        return islands;
    }

    public static List<Island> Classify(List<BspSubsector> subsectors, List<Sector> sectors, int sectorId = -1)
    {
        IslandId = 0;
        SectorIslandId = 0;
        List<Island> islands = new();

        foreach (BspSubsector subsector in subsectors)
        {
            if (ProcessedSubsectors.Contains(subsector)) 
                continue;

            Island island = new(sectorId == -1 ? IslandId++ : SectorIslandId++);
            islands.Add(island);
            if (sectorId != -1)
                island.SectorId = sectorId;
            TraverseSubsectors(subsector, island, ProcessedSubsectors, sectors, sectorId);
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
        List<Sector> sectors, int sectorId)
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
            {
                var sector = sectors[subsector.SectorId.Value];
                sector.Island = island;
            }

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
