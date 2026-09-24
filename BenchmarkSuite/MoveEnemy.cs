using System;
using BenchmarkDotNet.Attributes;
using Helion.World.Entities;
using Helion.Tests.Unit.GameAction;
using Helion.World.Impl.SinglePlayer;

namespace BenchmarkSuite;

public class MoveEnemy
{
    private readonly SinglePlayerWorld World;

    public MoveEnemy()
    {
        World = WorldAllocator.LoadMap("Resources/testmove.zip", "testmove.wad", "MAP01", Guid.NewGuid().ToString(), (world) => { });
    }

    [Benchmark]
    public void MoveEnemies()
    {
        for (int i = 0; i < 100; i++)
        {
            for (var entity = World.EntityManager.Head; entity != null; entity = entity.Next)
            {
                entity.SetTarget(World.Player);
                entity.SetMoveDirection(Entity.MoveDir.South);
                entity.SetNewChaseDirection();
            }
        }
    }
}