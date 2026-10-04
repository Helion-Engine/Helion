using BenchmarkDotNet.Attributes;
using Helion.Tests.Unit.GameAction;
using Helion.World.Entities;
using Helion.World.Entities.Definition.States;
using Helion.World.Impl.SinglePlayer;
using System;
using System.Collections.Generic;

namespace BenchmarkSuite;

public class EntityOnEntityCollisionWithMissile
{
    private readonly SinglePlayerWorld World;
    private readonly List<Entity> Monsters = new(8192);

    public EntityOnEntityCollisionWithMissile()
    {
        World = WorldAllocator.LoadMap("Resources/1024enemies.zip", "1024enemies.wad", "MAP01", Guid.NewGuid().ToString(), (world) =>
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
        for (int i = 0; i < 100; i++)
        {
            foreach (var entity in Monsters)
            {
                EntityActionFunctions.A_TroopAttack(entity);
                entity.SetNewChaseDirection();
            }
        }
    }
}