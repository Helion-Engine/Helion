using BenchmarkDotNet.Attributes;
using Helion.Tests.Unit.GameAction;
using Helion.World.Entities;
using Helion.World.Impl.SinglePlayer;
using System;
using System.Collections.Generic;

namespace BenchmarkSuite;

public class EntityMoveGeneral
{
    private readonly SinglePlayerWorld World;
    private readonly List<Entity> Monsters = new(8192);

    public EntityMoveGeneral()
    {
        World = WorldAllocator.LoadMap("Resources/idumea.zip", "idumea.wad", "MAP01", Guid.NewGuid().ToString(), (world) =>
        {
            for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
            {
                if (!entity.Flags.CountKill() || (entity.ClosetFlags & ClosetFlags.MonsterCloset) != 0)
                    continue;

                entity.SetTarget(world.Player);
                entity.SetMoveDirection(Entity.MoveDir.South);
                Monsters.Add(entity);
            }
        });
    }

    [Benchmark]
    public void MoveEnemies()
    {
        for (int i = 0; i < 50; i++)
        {
            foreach (var entity in Monsters)
            {
                entity.SetNewChaseDirection();
            }
        }
    }
}