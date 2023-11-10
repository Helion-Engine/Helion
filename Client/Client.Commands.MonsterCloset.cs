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
        var islands = m_layerManager.WorldLayer.World.Geometry.Islands;
        List<MonsterClosetInfo> infoList = new();

        int count = 0;
        foreach (var island in islands)
        {
            if (type == ClosetType.Monster && !island.IsMonsterCloset)
                continue;

            if (type == ClosetType.VooDoo && !island.IsVooDooCloset)
                continue;

            IEnumerable<Sector> sectors = island.Subsectors.Where(x => x.Sector != null).Select(x => x.Sector).Distinct()!;
            infoList.Add(new(count, CountEntities(island), GetIslandBox(island),
                string.Join(", ", sectors.Select(x => x.Id))));
            count++;
        }

        string stringType = type == ClosetType.Monster ? "monster" : "voodoo"; 

        Log.Info($"Total {stringType} closets: {count}");
        if (type == ClosetType.Monster)
            Log.Info($"Total monsters: {infoList.Sum(x => x.MonsterCount)}");

        foreach (var info in infoList)
        {
            Log.Info($"{stringType} closet {info.Id}");
            if (type == ClosetType.Monster)
                Log.Info($"Monster count: {info.MonsterCount}");
            Log.Info($"Bounds: {info.Box}");
            Log.Info($"Sectors: {info.Sectors}");
        }

        int CountEntities(Island island)
        {
            int count = 0;
            HashSet<BspSubsector> subsectors = island.Subsectors.ToHashSet();

            for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
            {
                BspSubsector subsector = world.Geometry.BspTree.Find(entity.CenterPoint);
                if (subsectors.Contains(subsector))
                    count++;
            }
            return count;
        }

        Box2D GetIslandBox(Island island)
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
                if (subsector.Box.Min.Y > max.Y)
                    max.Y = subsector.Box.Max.Y;
            }

            return new Box2D(min, max);
        }
    }
}
