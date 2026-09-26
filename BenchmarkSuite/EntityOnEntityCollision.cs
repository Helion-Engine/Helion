using System;
using BenchmarkDotNet.Attributes;
using Helion.World.Entities;
using Helion.Tests.Unit.GameAction;
using Helion.World.Impl.SinglePlayer;

namespace BenchmarkSuite;

public class EntityOnEntityCollision
{
    private readonly SinglePlayerWorld World;

    public EntityOnEntityCollision()
    {
        World = WorldAllocator.LoadMap("Resources/1024enemies.zip", "1024enemies.wad", "MAP01", Guid.NewGuid().ToString(), (world) =>
        {
            for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
            {
                entity.SetTarget(world.Player);
                entity.SetMoveDirection(Entity.MoveDir.South);
            }
        });
    }

    [Benchmark]
    public void SetNewChaseDirection()
    {
        for (int i = 0; i < 100; i++)
        {
            for (var entity = World.EntityManager.Head; entity != null; entity = entity.Next)
            {
                entity.SetNewChaseDirection();
            }
        }
    }
}