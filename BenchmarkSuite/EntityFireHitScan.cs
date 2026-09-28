using BenchmarkDotNet.Attributes;
using Helion.Tests.Unit.GameAction;
using Helion.World.Entities;
using Helion.World.Impl.SinglePlayer;
using System;
using System.Collections.Generic;

namespace BenchmarkSuite;

public class EntityFireHitScan
{
    record struct ShootData(Entity Entity, double Angle);

    private readonly SinglePlayerWorld World;
    private readonly List<ShootData> Data = new(8192);

    public EntityFireHitScan()
    {
        World = WorldAllocator.LoadMap("Resources/idumea.zip", "idumea.wad", "MAP01", Guid.NewGuid().ToString(), (world) =>
        {
            var player = world.Player;
            foreach (var entity in world.GetMonstersNoCloset())
                Data.Add(new(entity, entity.Position.Angle(player.Position)));
        });
    }

    [Benchmark]
    public void FireHitScan()
    {
        for (int i = 0; i < 10; i++)
        {
            foreach (var data in Data)
                World.FireHitscan(data.Entity, data.Angle, 0, 8192, damage: 0);
        }
    }
}
