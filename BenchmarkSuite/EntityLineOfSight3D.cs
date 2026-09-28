using BenchmarkDotNet.Attributes;
using Helion.Tests.Unit.GameAction;
using Helion.World.Entities;
using Helion.World.Impl.SinglePlayer;
using System;
using System.Collections.Generic;

namespace BenchmarkSuite;

public class EntityLineOfSight3D
{
    private readonly SinglePlayerWorld World;
    private readonly List<Entity> Monsters = new(8192);

    public EntityLineOfSight3D()
    {
        World = WorldAllocator.LoadMap("Resources/helion-castle.zip", "helion-castle.wad", "MAP01", Guid.NewGuid().ToString(), (world) =>
        {
            Monsters.AddRange(world.GetMonstersNoCloset());
        });
    }

    [Benchmark]
    public void LineOfSight()
    {
        var player = World.Player;
        for (int i = 0; i < 160; i++)
        {
            foreach (var entity in Monsters)
                World.CheckLineOfSight(entity, player);
        }
    }
}
