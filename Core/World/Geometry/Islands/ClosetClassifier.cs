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
    public static void Classify(WorldBase world)
    {
        PopulateLookups(world, out var islandToEntities, out var entityToSubsector);

        for (int i = 0; i < world.Geometry.Islands.Count; i++)
        {
            Island island = world.Geometry.Islands[i];
            if (!islandToEntities.TryGetValue(island, out var entities))
                continue;

            SetCloset(island, world, entities, entityToSubsector);      

            if (island.IsMonsterCloset)
            {
                foreach (Entity entity in islandToEntities[island])
                    entity.InMonsterCloset = true;
            }
        }
    }

    private static void PopulateLookups(WorldBase world, out Dictionary<Island, List<Entity>> islandToEntity, 
        out Dictionary<int, BspSubsector> entityToSubsector)
    {
        islandToEntity = new();
        entityToSubsector = new();
        foreach (Island island in world.Geometry.Islands)
            islandToEntity[island] = new();

        for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
        {
            BspSubsector subsector = world.Geometry.BspTree.Find(entity.CenterPoint);
            List<Entity> entities = islandToEntity[subsector.Island];
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
