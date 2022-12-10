using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using System;
using System.Collections.Generic;

namespace Helion.World.Geometry.Islands;

/// <summary>
/// A helper class that classifies geometry into islands.
/// </summary>
public static class IslandClassifier
{
    public static List<Island> Classify(List<Line> lines)
    {
        int islandId = 0;
        List<Island> islands = new();
        HashSet<Line> processedLines = new();
        HashSet<Sector> processedSectors = new();

        for (int i = 0; i < lines.Count; i++)
        {
            Line line = lines[i];
            if (processedLines.Contains(line))
                continue;

            Island island = new(islandId++);
            RecursivelyTraverse(line, island, processedLines, processedSectors);
            islands.Add(island);
        }

        return islands;
    }

    private static void RecursivelyTraverse(Line line, Island island, HashSet<Line> processedLines, HashSet<Sector> processedSectors)
    {
        foreach (Sector sector in line.Sectors)
        {
            if (processedSectors.Contains(sector)) 
                continue;

            sector.Island = island;
            processedSectors.Add(sector);
            island.Sectors.Add(sector);

            foreach (Line sectorLine in sector.Lines)
            {
                if (processedLines.Contains(sectorLine))
                    continue;

                sectorLine.Island = island;
                island.Lines.Add(sectorLine);
                processedLines.Add(sectorLine);

                RecursivelyTraverse(sectorLine, island, processedLines, processedSectors);
            }
        }
    }
}
