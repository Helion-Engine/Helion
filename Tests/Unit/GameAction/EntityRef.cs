using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Impl.SinglePlayer;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class EntityRef
    {
        private const string Resource = "Resources/box.zip";
        private const string File = "box.wad";

        private void WorldInit(SinglePlayerWorld world)
        {
            CheatManager.Instance.ActivateCheat(world.Player, CheatType.God);
        }

        [Fact(DisplayName = "Test dispose target")]
        public void TargetRef()
        {
            var world = WorldAllocator.LoadMap(Resource, File, "MAP01", WorldInit, IWadType.Doom2);
            var lostSoul1 = GameActions.CreateEntity(world, "LostSoul", new Vec3D(-256, -64, 0));
            var lostSoul2 = GameActions.CreateEntity(world, "LostSoul", new Vec3D(-256, -64, 0));
            List<Entity> entities1 = new();
            List<Entity> entities2 = new();

            for (int i = 0; i < 10; i++)
            {
                var zombie = GameActions.CreateEntity(world, "ZombieMan", new Vec3D(-256, -64, 0));
                zombie.SetTarget(lostSoul1);
                entities1.Add(zombie);
            }

            // One reference list to the lost soul
            WeakEntity.WeakEntities.Count.Should().Be(1);

            for (int i = 0; i < 10; i++)
            {
                var zombie = GameActions.CreateEntity(world, "ZombieMan", new Vec3D(-256, -64, 0));
                zombie.SetTarget(lostSoul2);
                entities2.Add(zombie);
            }

            WeakEntity.WeakEntities.Count.Should().Be(2);
            WeakEntity.WeakEntities[lostSoul1.Id].Count.Should().Be(10);
            WeakEntity.WeakEntities[lostSoul2.Id].Count.Should().Be(10);
            DataCache.Instance.WeakEntitiesCount.Should().Be(0);
            DataCache.Instance.WeakEntitiesListCount.Should().Be(0);

            lostSoul1.Kill(null);
            GameActions.TickWorld(world, 200);

            WeakEntity.WeakEntities.Count.Should().Be(1);
            WeakEntity.WeakEntities[lostSoul2.Id].Count.Should().Be(10);
            DataCache.Instance.WeakEntitiesCount.Should().Be(0);
            DataCache.Instance.WeakEntitiesListCount.Should().Be(1);

            foreach (var entity in entities1)
                entity.Target.Entity.Should().BeNull();

            foreach (var entity in entities2)
            {
                entity.Target.Entity.Should().NotBeNull();
                entity.Target.Entity!.Should().Be(lostSoul2);
            }

            lostSoul2.Kill(null);
            GameActions.TickWorld(world, 200);

            WeakEntity.WeakEntities.Count.Should().Be(0);
            DataCache.Instance.WeakEntitiesCount.Should().Be(0);
            DataCache.Instance.WeakEntitiesListCount.Should().Be(2);

            foreach (var entity in entities2)
                entity.Target.Entity.Should().BeNull();

            foreach (var entity in entities1.Union(entities2))
                entity.Dispose();

            DataCache.Instance.WeakEntitiesCount.Should().Be(20);
        }
    }
}
