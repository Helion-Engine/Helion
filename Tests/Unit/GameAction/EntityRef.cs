using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Impl.SinglePlayer;
using System;
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
            world.CheatManager.ActivateCheat(world.Player, CheatType.God);
            DataCache.Instance.ClearWeakEntities();
            DataCache.Instance.ClearWeakEntityLists();
        }

        [Fact(DisplayName = "Test dispose target")]
        public void TargetRef()
        {
            var world = WorldAllocator.LoadMap(Resource, File, "MAP01", Guid.NewGuid().ToString(), WorldInit, IWadType.Doom2);
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

            foreach (var entity in entities1)
                entity.Target.Entity.Should().Be(lostSoul1);

            // One reference list to the lost soul
            WeakEntity.ReferenceListCount().Should().Be(1);
            WeakEntity.GetReferences(lostSoul1)!.Count.Should().Be(10);

            for (int i = 0; i < 10; i++)
            {
                var zombie = GameActions.CreateEntity(world, "ZombieMan", new Vec3D(-256, -64, 0));
                zombie.SetTarget(lostSoul2);
                entities2.Add(zombie);
            }

            foreach (var entity in entities2)
                entity.Target.Entity.Should().Be(lostSoul2);

            WeakEntity.ReferenceListCount().Should().Be(2);
            WeakEntity.GetReferences(lostSoul1)!.Count.Should().Be(10);
            WeakEntity.GetReferences(lostSoul2)!.Count.Should().Be(10);
            DataCache.Instance.WeakEntitiesCount.Should().Be(0);
            DataCache.Instance.WeakEntitiesListCount.Should().Be(0);

            lostSoul1.Kill(null);
            GameActions.TickWorld(world, 200);

            WeakEntity.ReferenceListCount().Should().Be(1);
            WeakEntity.GetReferences(lostSoul2)!.Count.Should().Be(10);
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

            WeakEntity.ReferenceListCount().Should().Be(0);
            DataCache.Instance.WeakEntitiesCount.Should().Be(0);
            DataCache.Instance.WeakEntitiesListCount.Should().Be(2);

            foreach (var entity in entities2)
                entity.Target.Entity.Should().BeNull();

            foreach (var entity in entities1.Union(entities2))
                entity.Dispose();

            DataCache.Instance.WeakEntitiesCount.Should().Be(20);
        }

        [Fact(DisplayName = "Test dispose tracer")]
        public void TracerRef()
        {
            var world = WorldAllocator.LoadMap(Resource, File, "MAP01", Guid.NewGuid().ToString(), WorldInit, IWadType.Doom2);
            var lostSoul1 = GameActions.CreateEntity(world, "LostSoul", new Vec3D(-256, -64, 0));
            List<Entity> entities = new();

            for (int i = 0; i < 10; i++)
            {
                var zombie = GameActions.CreateEntity(world, "ZombieMan", new Vec3D(-256, -64, 0));
                zombie.SetTracer(lostSoul1);
                entities.Add(zombie);
            }

            foreach (var entity in entities)
                entity.Tracer.Entity.Should().Be(lostSoul1);

            WeakEntity.ReferenceListCount().Should().Be(1);
            WeakEntity.GetReferences(lostSoul1)!.Count.Should().Be(10);

            lostSoul1.Kill(null);
            GameActions.TickWorld(world, 200);

            WeakEntity.ReferenceListCount().Should().Be(0);
            DataCache.Instance.WeakEntitiesCount.Should().Be(0);
            DataCache.Instance.WeakEntitiesListCount.Should().Be(1);

            foreach (var entity in entities)
                entity.Tracer.Entity.Should().BeNull();

            foreach (var entity in entities)
                entity.Dispose();

            DataCache.Instance.WeakEntitiesCount.Should().Be(10);
        }

        [Fact(DisplayName = "Test dispose target and tracer")]
        public void TargetAndTracerRef()
        {
            var world = WorldAllocator.LoadMap(Resource, File, "MAP01", Guid.NewGuid().ToString(), WorldInit, IWadType.Doom2);
            var lostSoul1 = GameActions.CreateEntity(world, "LostSoul", new Vec3D(-256, -64, 0));
            var lostSoul2 = GameActions.CreateEntity(world, "LostSoul", new Vec3D(-256, -64, 0));
            List<Entity> entities1 = new();
            List<Entity> entities2 = new();

            for (int i = 0; i < 10; i++)
            {
                var zombie = GameActions.CreateEntity(world, "ZombieMan", new Vec3D(-256, -64, 0));
                zombie.SetTarget(lostSoul1);
                zombie.SetTracer(lostSoul2);
                entities1.Add(zombie);
            }

            foreach (var entity in entities1)
            {
                entity.Target.Entity!.Should().Be(lostSoul1);
                entity.Tracer.Entity!.Should().Be(lostSoul2);
            }

            WeakEntity.ReferenceListCount().Should().Be(2);
            WeakEntity.GetReferences(lostSoul1)!.Count.Should().Be(10);
            WeakEntity.GetReferences(lostSoul2)!.Count.Should().Be(10);

            for (int i = 0; i < 10; i++)
            {
                var zombie = GameActions.CreateEntity(world, "ZombieMan", new Vec3D(-256, -64, 0));
                zombie.SetTarget(lostSoul2);
                zombie.SetTracer(lostSoul1);
                entities2.Add(zombie);
            }

            foreach (var entity in entities2)
            {
                entity.Target.Entity!.Should().Be(lostSoul2);
                entity.Tracer.Entity!.Should().Be(lostSoul1);
            }

            WeakEntity.ReferenceListCount().Should().Be(2);
            WeakEntity.GetReferences(lostSoul1)!.Count.Should().Be(20);
            WeakEntity.GetReferences(lostSoul2)!.Count.Should().Be(20);
            DataCache.Instance.WeakEntitiesCount.Should().Be(0);
            DataCache.Instance.WeakEntitiesListCount.Should().Be(0);

            lostSoul1.Kill(null);
            GameActions.TickWorld(world, 200);

            WeakEntity.ReferenceListCount().Should().Be(1);
            WeakEntity.GetReferences(lostSoul2)!.Count.Should().Be(20);
            DataCache.Instance.WeakEntitiesCount.Should().Be(0);
            DataCache.Instance.WeakEntitiesListCount.Should().Be(1);

            foreach (var entity in entities1)
            {
                entity.Target.Entity.Should().BeNull();
                entity.Tracer.Entity.Should().NotBeNull();
                entity.Tracer.Entity!.Should().Be(lostSoul2);
            }

            foreach (var entity in entities2)
            {
                entity.Target.Entity.Should().NotBeNull();
                entity.Target.Entity!.Should().Be(lostSoul2);
                entity.Tracer.Entity.Should().BeNull();
            }

            lostSoul2.Kill(null);
            GameActions.TickWorld(world, 200);

            WeakEntity.ReferenceListCount().Should().Be(0);
            DataCache.Instance.WeakEntitiesCount.Should().Be(0);
            DataCache.Instance.WeakEntitiesListCount.Should().Be(2);

            foreach (var entity in entities2)
            {
                entity.Target.Entity.Should().BeNull();
                entity.Tracer.Entity.Should().BeNull();
            }

            foreach (var entity in entities1.Union(entities2))
                entity.Dispose();

            DataCache.Instance.WeakEntitiesCount.Should().Be(40);
        }

        [Fact(DisplayName = "Set weak entity references")]
        public void SetReferences()
        {
            var world = WorldAllocator.LoadMap(Resource, File, "MAP01", Guid.NewGuid().ToString(), WorldInit, IWadType.Doom2);
            var lostSoul = GameActions.CreateEntity(world, "LostSoul", new Vec3D(-256, -64, 0));
            var zombie = GameActions.CreateEntity(world, "ZombieMan", new Vec3D(-256, -64, 0));

            zombie.SetTarget(lostSoul);
            zombie.Target.Entity.Should().Be(lostSoul);
            zombie.Tracer.Entity.Should().BeNull();
            zombie.OnEntity.Entity.Should().BeNull();
            zombie.OverEntity.Entity.Should().BeNull();
            zombie.Owner.Entity.Should().BeNull();

            zombie.SetTarget(null);
            zombie.SetTracer(lostSoul);
            zombie.Target.Entity.Should().BeNull();
            zombie.Tracer.Entity.Should().Be(lostSoul);
            zombie.OnEntity.Entity.Should().BeNull();
            zombie.OverEntity.Entity.Should().BeNull();
            zombie.Owner.Entity.Should().BeNull();

            zombie.SetTracer(null);
            zombie.SetOnEntity(lostSoul);
            zombie.Target.Entity.Should().BeNull();
            zombie.Tracer.Entity.Should().BeNull();
            zombie.OnEntity.Entity.Should().Be(lostSoul);
            zombie.OverEntity.Entity.Should().BeNull();
            zombie.Owner.Entity.Should().BeNull();

            zombie.SetOnEntity(null);
            zombie.SetOverEntity(lostSoul);
            zombie.Target.Entity.Should().BeNull();
            zombie.Tracer.Entity.Should().BeNull();
            zombie.OnEntity.Entity.Should().BeNull();
            zombie.OverEntity.Entity.Should().Be(lostSoul);
            zombie.Owner.Entity.Should().BeNull();

            zombie.SetOverEntity(null);
            zombie.SetOwner(lostSoul);
            zombie.Target.Entity.Should().BeNull();
            zombie.Tracer.Entity.Should().BeNull();
            zombie.OnEntity.Entity.Should().BeNull();
            zombie.OverEntity.Entity.Should().BeNull();
            zombie.Owner.Entity.Should().Be(lostSoul);

            zombie.SetOwner(null);
            zombie.Target.Entity.Should().BeNull();
            zombie.Tracer.Entity.Should().BeNull();
            zombie.OnEntity.Entity.Should().BeNull();
            zombie.OverEntity.Entity.Should().BeNull();
            zombie.Owner.Entity.Should().BeNull();
        }

        [Fact(DisplayName = "Do not free default instance to cache")]
        public void DefaultFreeCheck()
        {
            var world = WorldAllocator.LoadMap(Resource, File, "MAP01", Guid.NewGuid().ToString(), WorldInit, IWadType.Doom2);
            var lostSoul = GameActions.CreateEntity(world, "LostSoul", new Vec3D(-256, -64, 0));
            lostSoul.Kill(null);
            GameActions.TickWorld(world, 200);

            lostSoul.IsDisposed.Should().BeTrue();
            DataCache.Instance.WeakEntitiesCount.Should().Be(0);

            lostSoul = GameActions.CreateEntity(world, "LostSoul", new Vec3D(-256, -64, 0));

            // Setting default weak references to null should leave them default.
            lostSoul.SetTarget(null);
            lostSoul.SetTracer(null);
            lostSoul.SetOnEntity(null);
            lostSoul.SetOverEntity(null);
            lostSoul.SetOwner(null);

            lostSoul.Kill(null);
            GameActions.TickWorld(world, 200);

            lostSoul.IsDisposed.Should().BeTrue();
            DataCache.Instance.WeakEntitiesCount.Should().Be(0);
        }

        [Fact(DisplayName = "Set clear and change references")]
        public void SetClearChange()
        {
            var world = WorldAllocator.LoadMap(Resource, File, "MAP01", Guid.NewGuid().ToString(), WorldInit, IWadType.Doom2);
            var lostSoul1 = GameActions.CreateEntity(world, "LostSoul", new Vec3D(-256, -64, 0));
            var caco1 = GameActions.CreateEntity(world, "Cacodemon", new Vec3D(-256, -64, 0));
            var zombie1 = GameActions.CreateEntity(world, "ZombieMan", new Vec3D(-256, -64, 0));
            var zombie2 = GameActions.CreateEntity(world, "ZombieMan", new Vec3D(-256, -64, 0));

            zombie1.SetTarget(lostSoul1);
            zombie2.SetTarget(lostSoul1);
            zombie1.SetTarget(null);
            zombie1.SetTarget(lostSoul1);
            zombie1.SetTarget(null);
            zombie1.SetTarget(caco1);

            var lostSoulReferences = WeakEntity.GetReferences(lostSoul1);
            lostSoulReferences.Should().NotBeNull();
            lostSoulReferences!.Count.Should().Be(1);
            lostSoulReferences!.First().Entity.Should().Be(lostSoul1);

            lostSoul1.Kill(null);
            GameActions.TickWorld(world, 200);

            zombie1.Target.Entity.Should().Be(caco1);
            zombie2.Target.Entity.Should().BeNull();

            lostSoulReferences = WeakEntity.GetReferences(lostSoul1);
            lostSoulReferences.Should().BeNull();

            var lostSoul2 = GameActions.CreateEntity(world, "LostSoul", new Vec3D(-256, -64, 0));
            zombie1.SetTarget(lostSoul2);
            zombie2.SetTarget(lostSoul2);

            lostSoulReferences = WeakEntity.GetReferences(lostSoul2);
            lostSoulReferences.Should().NotBeNull();
            lostSoulReferences!.Count.Should().Be(2);
            var node = lostSoulReferences!.First;
            while (node != null)
            {
                node.Value.Entity.Should().Be(lostSoul2);
                node = node.Next;
            }

            zombie2.SetTarget(caco1);
            lostSoul2.Kill(null);
            GameActions.TickWorld(world, 200);

            zombie2.Target.Entity.Should().Be(caco1);
            zombie1.Target.Entity.Should().BeNull();
        }

        [Fact(DisplayName = "Dispose ref")]
        public void DisposeRef()
        {
            var world = WorldAllocator.LoadMap(Resource, File, "MAP01", Guid.NewGuid().ToString(), WorldInit, IWadType.Doom2);
            var lostSoul1 = GameActions.CreateEntity(world, "LostSoul", new Vec3D(-256, -64, 0));
            var caco1 = GameActions.CreateEntity(world, "Cacodemon", new Vec3D(-256, -64, 0));

            lostSoul1.SetTarget(caco1);
            caco1.SetTarget(lostSoul1);

            DataCache.Instance.WeakEntitiesCount.Should().Be(0);
            DataCache.Instance.WeakEntitiesListCount.Should().Be(0);
            lostSoul1.Kill(null);
            GameActions.TickWorld(world, 200);

            WeakEntity.GetReferences(lostSoul1).Should().BeNull();
            WeakEntity.GetReferences(caco1).Should().BeNull();

            // The lost soul had the only reference to the caco. The caco reference should be free.
            DataCache.Instance.WeakEntitiesCount.Should().Be(1);
            // Both lists should be free since the lost soul had the reference to the caco and the caco referenced the lost soul back.
            // Because the lost soul is dead both lists are no longer needed.
            DataCache.Instance.WeakEntitiesListCount.Should().Be(2);
        }
    }
}
