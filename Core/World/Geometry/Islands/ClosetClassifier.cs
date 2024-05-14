using Helion.Geometry.Vectors;
using Helion.Util.Loggers;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using System.Collections.Generic;
using System.Linq;

namespace Helion.World.Geometry.Islands;

public static class ClosetClassifier
{
    // Assumes entities and geometry have been populated. Should be done as
    // a final post-processing step.
    public static void Classify(WorldBase world, bool isFromSave)
    {
        if (world.SameAsPreviousMap)
        {
            for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
            {
                var subsector = world.Geometry.BspTree.Subsectors[entity.Subsector.Id];
                if (subsector.IslandId < 0 || subsector.IslandId >= world.Geometry.IslandGeometry.Islands.Count)
                    continue;
                var island = world.Geometry.IslandGeometry.Islands[subsector.IslandId];
                if (!isFromSave)
                {
                    if (island.IsMonsterCloset)
                        entity.ClosetFlags |= ClosetFlags.MonsterCloset;
                    continue;
                }

                // Saved misclassification?
                if (!island.IsMonsterCloset && entity.ClosetFlags != ClosetFlags.None)
                    entity.ClearMonsterCloset();
            }
            return;
        }

        PopulateLookups(world, out var islandToEntities, out var entityToSubsector);

        for (int i = 0; i < world.Geometry.IslandGeometry.Islands.Count; i++)
        {
            Island island = world.Geometry.IslandGeometry.Islands[i];
            if (!islandToEntities.TryGetValue(island.Id, out var entities))
                continue;

            SetCloset(island, world, entities, entityToSubsector);      

            if (island.IsMonsterCloset)
            {
                foreach (Entity entity in islandToEntities[island.Id])
                    entity.ClosetFlags |= ClosetFlags.MonsterCloset;
            }

            if (island.IsMonsterCloset || island.IsVooDooCloset)
            {
                foreach (var subsector in island.Subsectors)
                {
                    if (!subsector.SectorId.HasValue)
                        continue;
                    var sectorIslands = world.Geometry.IslandGeometry.SectorIslands[subsector.SectorId.Value];
                    foreach (var sectorIsland in sectorIslands)
                    {
                        island.IsVooDooCloset = island.IsVooDooCloset;
                        island.IsMonsterCloset = island.IsMonsterCloset;
                    }
                }
            }
        }

        var closets = world.Geometry.IslandGeometry.Islands.Where(x => x.IsVooDooCloset).ToList();

        for (int i = 0; i < world.Geometry.IslandGeometry.SectorIslands.Length; i++)
        {
            var islands = world.Geometry.IslandGeometry.SectorIslands[i];
            var sector = world.Sectors[i];
            foreach (var island in islands)
            {
                island.IsVooDooCloset = sector.Island.IsVooDooCloset;
                island.IsMonsterCloset = sector.Island.IsMonsterCloset;
            }
        }
    }

    private static void PopulateLookups(WorldBase world, out Dictionary<int, List<Entity>> islandToEntity, 
        out Dictionary<int, BspSubsector> entityToSubsector)
    {
        islandToEntity = new();
        entityToSubsector = new();
        foreach (Island island in world.Geometry.IslandGeometry.Islands)
            islandToEntity[island.Id] = new();

        for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
        {
            var subsector = world.Geometry.BspTree.Subsectors[entity.Subsector.Id];
            var entities = islandToEntity[subsector.IslandId];
            entities.Add(entity);
            entityToSubsector[entity.Id] = subsector;
        }
    }

    private static void SetCloset(Island island, WorldBase world, List<Entity> entities,
        Dictionary<int, BspSubsector> entityToSubsector)
    {
        // Monster closets are simple, should not have a ton of lines.
        if (island.LineIds.Count > 300)
            return;

        bool monsterCloset = true;
        bool voodooCloset = true;

        bool hasNonMonsterClosetSpecial = false;
        for (int i = 0; i < island.LineIds.Count; i++)
        {
            var line = world.Lines[island.LineIds[i]];
            if (line.HasSpecial && !line.Special.IsTeleport() && !line.Special.IsPlaneScroller())
            {
                monsterCloset = false;
                hasNonMonsterClosetSpecial = true;
                break;
            }
        }

        int monsterCount = 0;
        int playerCount = 0;
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (!entityToSubsector.TryGetValue(entity.Id, out var subsector))
                continue;

            // Anything not a monster is not a monster closet.
            if (entity.Flags.CountKill)
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
