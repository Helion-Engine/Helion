using Helion.Geometry.Vectors;
using Helion.Util.Consoles.Commands;
using Helion.Util.Consoles;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Geometry.Islands;
using Helion.World.Geometry.Sectors;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry.Boxes;
using Helion.Util.Loggers;
using Helion.World;

namespace Helion.Client;

public partial class Client
{
    enum ClosetType
    {
        Monster,
        VooDoo
    }

    [ConsoleCommand("voodooclosets", "Prints the voodoo doll closets in the map.")]
    private void PrintVooDooClosets(ConsoleCommandEventArgs args)
    {
        PrintClosets(ClosetType.VooDoo);
    }

    [ConsoleCommand("monsterclosets", "Prints the monster closets in the map.")]
    private void PrintMonsterClosets(ConsoleCommandEventArgs args)
    {
        PrintClosets(ClosetType.Monster);
    }

    private void PrintClosets(ClosetType type)
    {
        if (m_layerManager.WorldLayer == null)
            return;

        var world = m_layerManager.WorldLayer.World;
        var islands = m_layerManager.WorldLayer.World.Geometry.IslandGeometry.Islands;
        List<MonsterClosetInfo> infoList = new();

        int count = 0;
        foreach (var island in islands)
        {
            if (type == ClosetType.Monster && !island.IsMonsterCloset)
                continue;

            if (type == ClosetType.VooDoo && !island.IsVooDooCloset)
                continue;

            IEnumerable<Sector> sectors = island.Subsectors.Where(x => x.SectorId != null).Select(x => world.Sectors[x.SectorId!.Value]).Distinct()!;
            infoList.Add(new(count, CountEntities(world, island), island.Box,
                string.Join(", ", sectors.Select(x => x.Id))));
            count++;
        }

        string stringType = type == ClosetType.Monster ? "monster" : "voodoo";

        HelionLog.Info($"Total {stringType} closets: {count}");
        if (type == ClosetType.Monster)
            HelionLog.Info($"Total monsters: {infoList.Sum(x => x.MonsterCount)}");

        foreach (var info in infoList)
        {
            HelionLog.Info($"{stringType} closet {info.Id}");
            if (type == ClosetType.Monster)
                HelionLog.Info($"Monster count: {info.MonsterCount}");
            HelionLog.Info($"Bounds: {info.Box}");
            HelionLog.Info($"Sectors: {info.Sectors}");
        }
    }

    static int CountEntities(IWorld world, Island island)
    {
        int count = 0;
        for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
        {
            var subsector = world.Geometry.BspTree.Subsectors[entity.Subsector.Id];
            var findIsland = world.Geometry.IslandGeometry.Islands[subsector.IslandId];
            if (findIsland.Id == island.Id)
                count++;
        }
        return count;
    }
}
