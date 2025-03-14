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
public class IslandClassifier
{
    int IslandId = 0;
    int SectorIslandId = 0;
    int SubsectorCounter = 1;
    int LineCounter = 1;
    int[] ProcessedSubsectors = [];
    int[] VisitedLines = [];

    public List<Island>[] ClassifySectors(List<BspSubsector> subsectors, List<Sector> sectors, int lineCount)
    {
        List<Island>[] islands = new List<Island>[sectors.Count];
        var subsectorLookup = subsectors.Where(x => x.SectorId.HasValue).GroupBy(x => x.SectorId!.Value).ToDictionary(x => x.Key, x => x.ToList());

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

    public List<Island> Classify(List<BspSubsector> subsectors, List<Sector> sectors, int lineCount, int sectorId = -1)
    {
        IslandId = 0;
        SectorIslandId = 0;
        List<Island> islands = [];
        SubsectorCounter++;

        if (ProcessedSubsectors.Length < subsectors.Count)
            ProcessedSubsectors = new int[subsectors.Count];

        if (VisitedLines.Length < lineCount)
            VisitedLines = new int[lineCount];

        foreach (BspSubsector subsector in subsectors)
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

        return islands;
    }

    private void TraverseSubsectors(BspSubsector initialSubsector, Island island, 
        List<Sector> sectors, int sectorId)
    {
        LineCounter++;
        Stack<BspSubsector> subsectorsToVisit = new();
        subsectorsToVisit.Push(initialSubsector);

        while (subsectorsToVisit.Count > 0)
        {
            BspSubsector subsector = subsectorsToVisit.Pop();

            if (ProcessedSubsectors[subsector.Id] == SubsectorCounter)
                continue;

            ProcessedSubsectors[subsector.Id] = SubsectorCounter;
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
                if (seg.LineId != null && VisitedLines[seg.LineId.Value] != LineCounter)
                {
                    island.LineIds.Add(seg.LineId.Value);
                    VisitedLines[seg.LineId.Value] = LineCounter;
                }

                if (seg.Partner != null && ProcessedSubsectors[seg.Partner.Subsector.Id] != SubsectorCounter)
                {
                    if (sectorId == -1 || sectorId == seg.Partner.Subsector.SectorId)
                        subsectorsToVisit.Push(seg.Partner.Subsector);
                }
            }
        }
    }
}
