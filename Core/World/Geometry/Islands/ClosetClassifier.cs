using Helion.Geometry.Vectors;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using System.Collections.Generic;

namespace Helion.World.Geometry.Islands;

public static class ClosetClassifier
{
    public static void ClassifySameMap(WorldBase world)
    {
        for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
        {
            if (entity.Flags.Friendly())
                continue;

            var islandId = world.Geometry.SubsectorToIslandId[entity.SubsectorId];
            if (islandId < 0 || islandId >= world.Geometry.IslandGeometry.Islands.Count)
                continue;

            var island = world.Geometry.IslandGeometry.Islands[islandId];
            if (island.IsMonsterCloset)
                entity.ClosetFlags |= ClosetFlags.MonsterCloset;
        }
    }

    public static void Classify(WorldBase world, CompactBspTree bspTree)
    {
        PopulateLookups(world, bspTree, out var islandToEntities, out var entityToSubsector);

        for (int i = 0; i < world.Geometry.IslandGeometry.Islands.Count; i++)
        {
            Island island = world.Geometry.IslandGeometry.Islands[i];
            if (!islandToEntities.TryGetValue(island.Id, out var entities))
                continue;

            SetCloset(island, world, entities, entityToSubsector);

            if (island.IsMonsterCloset)
            {
                foreach (Entity entity in islandToEntities[island.Id])
                {
                    if (entity.Flags.Friendly())
                        continue;
                    entity.ClosetFlags |= ClosetFlags.MonsterCloset;
                }
            }
        }

        for (int i = 0; i < world.Geometry.IslandGeometry.SectorIslands.Length; i++)
        {
            var islands = world.Geometry.IslandGeometry.SectorIslands[i];
            foreach (var island in islands)
            {
                if (island.ParentIsland == null)
                    continue;
                island.IsVooDooCloset = island.ParentIsland.IsVooDooCloset;
                island.IsMonsterCloset = island.ParentIsland.IsMonsterCloset;
            }
        }
    }

    private static void PopulateLookups(WorldBase world, CompactBspTree bspTree, out Dictionary<int, List<Entity>> islandToEntity,
        out Dictionary<int, Subsector> entityToSubsector)
    {
        islandToEntity = [];
        entityToSubsector = [];
        foreach (Island island in world.Geometry.IslandGeometry.Islands)
            islandToEntity[island.Id] = [];

        for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
        {
            var subsector = bspTree.Subsectors[entity.SubsectorId];
            islandToEntity[subsector.IslandId].Add(entity);
            entityToSubsector[entity.Id] = subsector;
        }
    }

    private static void SetCloset(Island island, WorldBase world, List<Entity> entities,
        Dictionary<int, Subsector> entityToSubsector)
    {
        bool monsterCloset = true;
        bool voodooCloset = true;

        for (int i = 0; i < island.LineIds.Count; i++)
        {
            var line = world.Lines[island.LineIds[i]];
            if (line.HasSpecial && !line.Special.IsTeleport() && !line.Special.IsPlaneScroller())
            {
                monsterCloset = false;
                break;
            }
        }

        int monsterCount = 0;
        int playerCount = 0;
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (!entityToSubsector.TryGetValue(entity.Id, out _))
                continue;

            // Anything not a monster is not a monster closet.
            if (entity.Flags.CountKill())
                monsterCount++;
            else
                monsterCloset = false;

            if (entity.PlayerObj != null)
            {
                if (!entity.PlayerObj.IsVooDooDoll)
                    voodooCloset = false;
                playerCount++;
            }
        }

        island.IsMonsterCloset = monsterCloset && monsterCount > 0;
        island.IsVooDooCloset = !monsterCloset && voodooCloset && playerCount == 1;
    }

    // A "bridge" is a sector that connects two sections of the map, whereby
    // removal of the bridge would create two disjoint islands.
    private static HashSet<Sector> ClassifyBridges(List<Sector> sectors)
    {
        // Probably 99% of the bridges are four lines, but I've seen some that
        // are an L shaped bend, so this would support those too.
        const int MaxBridgeLines = 6;

        HashSet<Sector> bridges = new();
        HashSet<Vec2D> vertices = new();
        List<Line> twoSidedLines = new();

        foreach (Sector sector in sectors)
        {
            if (sector.Lines.Length > MaxBridgeLines)
                continue;
            if (!sector.AreFlatsStatic)
                continue;
            if (sector.Entities.Head != null)
                continue;

            // A bridge connects two sectors. This means it has exactly two connections
            // (2 two-sided lines), and they must not be touching (which means there are
            // exactly 4 vertices for both two-sided lines, since if they do touch, then
            // they share a vertex, and that means there are 3 unique vertices and not 4).
            twoSidedLines.Clear();
            foreach (var line in sector.Lines)
            {
                if (line.Back == null)
                    twoSidedLines.Add(line);
            }
            if (twoSidedLines.Count != 2)
                continue;

            vertices.Clear();
            foreach (var line in twoSidedLines)
            {
                vertices.Add(line.Segment.Start);
                vertices.Add(line.Segment.End);
            }

            if (vertices.Count != 4)
                continue;

            bridges.Add(sector);
        }

        return bridges;
    }
}
