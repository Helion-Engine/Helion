using Helion.Util.Container;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.World.Geometry.Islands;

/// <summary>
/// A classifier for monster closets, which are disjoint areas of the map
/// that the player cannot reach, and only monsters are in, and only allow
/// monsters out of them. Each monster closet is effectively an island.
/// </summary>
public static class MonsterClosets
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // Assumes entities and geometry have been populated. Should be done as
    // a final post-processing step.
    public static void Classify(WorldBase world)
    {
        foreach (Island island in world.Geometry.Islands)
            if (CalculateIfMonsterCloset(island, world))
                foreach (Entity entity in world.Entities.Enumerate())
                    entity.InMonsterCloset = true;
    }

    private static bool CalculateIfMonsterCloset(Island island, WorldBase world)
    {
        if (island.Lines.Count > 300)
            return false;

        foreach (Line line in island.Lines)
        {
            // We only allow teleport lines in monster closets.
            if (line.HasSpecial && !line.Special.IsTeleport())
                return false;
        }

        HashSet<BspSubsector> subsectors = island.Subsectors.ToHashSet();

        foreach (Entity entity in world.Entities.Enumerate())
        {
            BspSubsector subsector = FindSubsector(entity, world);
            if (!subsectors.Contains(subsector))
                continue;

            bool isMonster = entity.Flags.CountKill;
            if (!isMonster)
                return false;
        }

        return true;

        static BspSubsector FindSubsector(Entity entity, WorldBase world)
        {
            int index = world.BspTree.ToSubsectorIndex(entity.CenterPoint);
            return world.Geometry.BspTree.Subsectors[index];
        }
    }

    // A "bridge" is a sector that connects two sections of the map, whereby
    // removal of the bridge would create two disjoint islands.
    private static HashSet<Sector> ClassifyBridges(List<Sector> sectors)
    {
        // Probably 99% of the bridges are four lines, but I've seen some that
        // are an L shaped bend, so this would support those too.
        const int MaxBridgeLines = 6;

        HashSet<Sector> bridges = new();

        foreach (Sector sector in sectors)
        {
            if (sector.Lines.Count > MaxBridgeLines)
                continue;
            if (!sector.AreFlatsStatic)
                continue;
            if (sector.Entities.Head != null)
                continue;

            // A bridge connects two sectors. This means it has exactly two connections
            // (2 two-sided lines), and they must not be touching (which means there are
            // exactly 4 vertices for both two-sided lines, since if they do touch, then
            // they share a vertex, and that means there are 3 unique vertices and not 4).
            IEnumerable<Line> twoSidedLines = sector.Lines.Where(l => l.TwoSided);
            if (twoSidedLines.Count() != 2)
                continue;
            if (twoSidedLines.SelectMany(l => l.Vertices).Distinct().Count() != 4)
                continue;

            bridges.Add(sector);
        }

        return bridges;
    }
}
