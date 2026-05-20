
using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World.Entities;
using Helion.World.Impl.SinglePlayer;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class ThingIdLookup
{
    private readonly SinglePlayerWorld World;

    public ThingIdLookup()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Add and remove thing ids")]
    public void AddAndRemoveThingIds()
    {
        const int TidCount = 10;
        const int ThingCount = 5;
        var entities = new List<Entity>();
        string[] names = ["ZombieMan", "ShotgunGuy", "DoomImp", "Demon", "LostSoul"];
        for (int i = 0; i < TidCount; i++)
            for (int j = 0; j < ThingCount; j++)
                entities.Add(CreateEntity(names[i % names.Length], i + 1)); 

        for (int i = 0; i < TidCount; i++)
            AssertThingList(World.EntityManager.FindByTid(i + 1), names[i % names.Length], ThingCount);

        // Move to new tid starting with the last node
        var tidOneList = World.EntityManager.FindByTid(1).Reverse().ToList();
        for (int i = 0; i < ThingCount; i++)
        {
            World.EntityManager.SetThingId(tidOneList[i], 69);
            AssertThingList(World.EntityManager.FindByTid(1), [.. tidOneList.Skip(i + 1)]);
        }
        AssertThingList(World.EntityManager.FindByTid(69), tidOneList);

        var movedList = tidOneList.ToList();

        // Move to new tid starting with the start node
        var tidTwoList = World.EntityManager.FindByTid(2).ToList();
        for (int i = 0; i < ThingCount; i++)
        {
            World.EntityManager.SetThingId(tidTwoList[i], 69);
            AssertThingList(World.EntityManager.FindByTid(2), [.. tidTwoList.Skip(i + 1)]);
        }
        movedList.AddRange(tidTwoList);
        AssertThingList(World.EntityManager.FindByTid(69), movedList);

        // Move to new tid from middle
        var tidThreeList = World.EntityManager.FindByTid(3).ToList();
        var removeEntities = new List<Entity>() { tidThreeList[2], tidThreeList[3] };

        World.EntityManager.SetThingId(removeEntities[0], 69);
        World.EntityManager.SetThingId(removeEntities[1], 69);
        movedList.AddRange(removeEntities);
        AssertThingList(World.EntityManager.FindByTid(3), [.. tidThreeList.Except(removeEntities)]);
        AssertThingList(World.EntityManager.FindByTid(69), movedList);

        // Remove tid
        var tidFourList = World.EntityManager.FindByTid(4).ToList();
        foreach (var entity in tidFourList)
            World.EntityManager.SetThingId(entity, 0);
        AssertThingList(World.EntityManager.FindByTid(4), []);
        foreach (var entity in tidFourList)
            entity.ThingIdNode.Should().BeNull();

        var tidFiveList = World.EntityManager.FindByTid(5);
        World.EntityManager.Destroy(tidFiveList);
        AssertThingList(World.EntityManager.FindByTid(5), []);
    }

    private static void AssertThingList(LinkableList<Entity> entities, string className, int count)
    {
        var set = new HashSet<int>();
        for (var node = entities.Head; node != null; node = node.Next)
        {
            node.Value.ThingIdNode.Should().NotBeNull();
            node.Value.Definition.Name.EqualsIgnoreCase(className).Should().BeTrue();
            set.Add(node.Value.Id).Should().BeTrue();
        }
        set.Count.Should().Be(count);
    }

    private static void AssertThingList(LinkableList<Entity> entities, List<Entity> expected)
    {
        var set = new HashSet<int>();
        for (var node = entities.Head; node != null; node = node.Next)
        {
            node.Value.ThingIdNode.Should().NotBeNull();
            expected.Contains(node.Value).Should().BeTrue();
            set.Add(node.Value.Id).Should().BeTrue();
        }

        set.Count.Should().Be(expected.Count);
    }

    private Entity CreateEntity(string className, int tid)
    {
        var entity = GameActions.CreateEntity(World, className, Vec3D.Zero);
        World.EntityManager.SetThingId(entity, tid);
        return entity;
    }
}
