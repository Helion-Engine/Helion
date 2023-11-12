using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;

namespace Helion.Render.Common.World.SelfReferencing;

// A connected group of subsectors on both sides of a closed-loop self-referencing
// set of lines.
public class SelfRefSubsectorIsland
{
    public readonly int Id;
    public Sector EnclosingSector { get; private set; }
    private readonly List<int> m_subsectorIds;
    private readonly List<Line> m_borders;

    public IReadOnlyList<int> SubsectorIds => m_subsectorIds;

    public SelfRefSubsectorIsland(int id, HashSet<int> subsectorIds, HashSet<Line> borders)
    {
        Id = id;
        m_subsectorIds = subsectorIds.ToList();
        m_borders = borders.ToList();
        EnclosingSector = FindEnclosingSector(m_borders);
    }

    private static List<Line> ExtractLoop(Line line, HashSet<Line> availableLines)
    {
        Debug.Assert(availableLines.Contains(line), "Line already extracted from self reference loop finder");

        Dictionary<Vec2D, HashSet<Line>> graph = new();
        foreach (Line availableLine in availableLines)
        {
            foreach (Vec2D pos in new[] { availableLine.StartPosition, availableLine.EndPosition })
            {
                if (!graph.TryGetValue(pos, out HashSet<Line>? lines))
                {
                    lines = new();
                    graph[pos] = lines;
                }

                lines.Add(availableLine);
            }
        }
        
        // TODO
        
        throw new System.NotImplementedException();
    }

    private static List<Line> FindLargestLoop(List<List<Line>> loops)
    {
        List<Box2D> loopBounds = loops.Select(loop => loop.Aggregate(loop[0].Segment.Box, (b, line) => b.Combine(line.Segment.Box))).ToList();
        double largestArea = loopBounds.Max(loopBound => loopBound.Area);
        for (int i = 0; i < loops.Count; i++)
            if (loopBounds[i].Area == largestArea)
                return loops[i];

        throw new("Impossible condition, should have been one loop that was the largest");
    }

    private static Sector GetSectorFromLoop(List<Line> largestLoop)
    {
        // This is actually wrong, a tooth-shaped sector will break this.
        // The real solution is to walk the loop and find out it's direction,
        // and then grab the side that the loop encases.
        Box2D bound = largestLoop.Aggregate(largestLoop[0].Segment.Box, (b, line) => b.Combine(line.Segment.Box));
        Vec2D center = bound.Min + (bound.Sides / 2);
        Line line = largestLoop[0];
        if (line.Segment.OnRight(center))
            return line.Front.Sector;
        return line.Back?.Sector ?? line.Front.Sector;
    }

    private static Sector FindEnclosingSector(List<Line> borders)
    {
        HashSet<Line> availableLines = borders.ToHashSet();
        List<List<Line>> loops = new();
        
        foreach (Line line in borders)
        {
            if (!availableLines.Contains(line))
                continue;

            List<Line> loop = ExtractLoop(line, availableLines);
            loops.Add(loop);
        }

        List<Line> largestLoop = FindLargestLoop(loops);
        return GetSectorFromLoop(largestLoop);
    }
}