using BenchmarkDotNet.Attributes;
using Helion.Geometry.Vectors;
using Helion.Tests.Unit.GameAction;
using Helion.World.Impl.SinglePlayer;
using System;

namespace BenchmarkSuite;

public class EntityCreate
{
    private readonly SinglePlayerWorld World;

    public EntityCreate()
    {
        World = WorldAllocator.LoadMap("Resources/idumea.zip", "idumea.wad", "MAP01", Guid.NewGuid().ToString(), (world) =>
        {
            world.DataCache.SetEntitiesForMapLoad(16384);
        });
    }

    [Benchmark]
    public void CreateEntities()
    {
        var impBall = World.EntityManager.DefinitionComposer.GetByName("DoomImpBall");
        var pos = new Vec3D(-65000, -65000, 32);
        for (int i = 0; i < 16000; i++)
        {
            World.EntityManager.Create(impBall, pos, 0, 0, 0, default);
            pos.X += 32;

            if (pos.X >= 65000)
            {
                pos.X = 0;
                pos.Y += 32;
            }
        }
    }
}
