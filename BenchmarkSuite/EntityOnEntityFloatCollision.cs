using BenchmarkDotNet.Attributes;
using Helion.Tests.Unit.GameAction;
using Helion.World.Entities;
using Helion.World.Impl.SinglePlayer;
using System;
using System.Collections.Generic;

namespace BenchmarkSuite;

public class EntityOnEntityFloatCollision
{
    private readonly SinglePlayerWorld World;
    private readonly List<Entity> Monsters = new(8192);

    public EntityOnEntityFloatCollision()
    {
        World = WorldAllocator.LoadMap("Resources/floatmove.zip", "floatmove.wad", "MAP01", Guid.NewGuid().ToString(), (world) =>
        {
            foreach (var entity in world.GetMonstersNoCloset())
            {
                entity.SetTarget(world.Player);
                entity.SetMoveDirection(Entity.MoveDir.South);
                Monsters.Add(entity);
            }
        });
    }

    [Benchmark]
    public void SetNewChaseDirection()
    {
        var player = World.Player;
        for (int i = 0; i < 50; i++)
        {
            player.Position.Z += i * 16;
            foreach (var entity in Monsters)
                entity.SetNewChaseDirection();
        }
    }
}
