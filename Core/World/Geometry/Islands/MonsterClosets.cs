using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
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
    // Assumes entities and geometry have been populated. Should be done as
    // a final post-processing step.
    public static void Classify(WorldBase world)
    {
        Dictionary<Island, List<Entity>> islandToEntities = PopulateEntityToIsland(world);

        foreach (Island island in world.Geometry.Islands)
        {
            if (!CalculateIfMonsterCloset(island, world))
                continue;

            island.IsMonsterCloset = true;

            foreach (Entity entity in islandToEntities[island])
                entity.InMonsterCloset = true;
        }
    }

    private static Dictionary<Island, List<Entity>> PopulateEntityToIsland(WorldBase world)
    {
        Dictionary<Island, List<Entity>> result = new();

        foreach (Island island in world.Geometry.Islands)
            result[island] = new();

        foreach (Entity entity in world.Entities.Enumerate())
        {
            BspSubsector subsector = world.Geometry.BspTree.Find(entity.CenterPoint);
            List<Entity> entities = result[subsector.Island];
            entities.Add(entity);
        }

        return result;
    }

    private static bool CalculateIfMonsterCloset(Island island, WorldBase world)
    {
        // Monster closets are simple, should not have a ton of lines.
        if (island.Lines.Count > 300)
            return false;

        foreach (Line line in island.Lines)
        {
            if (line.HasSpecial && !line.Special.IsTeleport() && !line.Special.IsPlaneScroller())
                return false;
        }

        HashSet<BspSubsector> subsectors = island.Subsectors.ToHashSet();
        int monsterCount = 0;

        foreach (Entity entity in world.Entities.Enumerate())
        {
            BspSubsector subsector = world.Geometry.BspTree.Find(entity.CenterPoint);
            if (!subsectors.Contains(subsector))
                continue;

            // Anything not a monster is not a monster closet.
            bool isMonster = entity.Flags.CountKill;
            if (!isMonster)
                return false;

            monsterCount++;
        }

        return monsterCount > 0;
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
