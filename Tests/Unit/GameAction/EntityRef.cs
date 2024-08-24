﻿using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World;
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
        private const string File = "box.WAD";

        private void WorldInit(SinglePlayerWorld world)
        {
            world.CheatManager.ActivateCheat(world.Player, CheatType.God);
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

            for (int i = 0; i < 10; i++)
            {
                var zombie = GameActions.CreateEntity(world, "ZombieMan", new Vec3D(-256, -64, 0));
                zombie.SetTarget(lostSoul2);
                entities2.Add(zombie);
            }

            foreach (var entity in entities2)
                entity.Target.Entity.Should().Be(lostSoul2);

            lostSoul1.Kill(null);
            GameActions.TickWorld(world, 200);

            foreach (var entity in entities1)
                entity.Target.Entity.Should().BeNull();

            foreach (var entity in entities2)
            {
                entity.Target.Entity.Should().NotBeNull();
                entity.Target.Entity!.Should().Be(lostSoul2);
            }

            lostSoul2.Kill(null);
            GameActions.TickWorld(world, 200);

            foreach (var entity in entities2)
                entity.Target.Entity.Should().BeNull();
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

            lostSoul1.Kill(null);
            GameActions.TickWorld(world, 200);

            foreach (var entity in entities)
                entity.Tracer.Entity.Should().BeNull();
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

            lostSoul1.Kill(null);
            GameActions.TickWorld(world, 200);

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

            foreach (var entity in entities2)
            {
                entity.Target.Entity.Should().BeNull();
                entity.Tracer.Entity.Should().BeNull();
            }

            foreach (var entity in entities1.Union(entities2))
                entity.Dispose();

            foreach (var entity in entities1.Union(entities2))
            {
                entity.Target.Entity.Should().BeNull();
                entity.Tracer.Entity.Should().BeNull();
            }
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
            lostSoul.Target.Should().Be(WeakEntity.Default);
            lostSoul.Tracer.Should().Be(WeakEntity.Default);
            lostSoul.OnEntity.Should().Be(WeakEntity.Default);
            lostSoul.OverEntity.Should().Be(WeakEntity.Default);
            lostSoul.Owner.Should().Be(WeakEntity.Default);
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
            zombie1.Target.Entity.Should().Be(lostSoul1);
            zombie2.SetTarget(lostSoul1);
            zombie1.SetTarget(null);
            zombie1.Target.Entity.Should().BeNull();
            zombie1.Target.Should().Be(WeakEntity.Default);
            zombie1.SetTarget(lostSoul1);
            zombie1.SetTarget(null);
            zombie1.SetTarget(caco1);

            zombie1.Target.Entity.Should().Be(caco1);
            zombie2.Target.Entity.Should().Be(lostSoul1);

            lostSoul1.Kill(null);
            GameActions.TickWorld(world, 200);

            zombie1.Target.Entity.Should().Be(caco1);
            zombie2.Target.Entity.Should().BeNull();

            var lostSoul2 = GameActions.CreateEntity(world, "LostSoul", new Vec3D(-256, -64, 0));
            zombie1.SetTarget(lostSoul2);
            zombie2.SetTarget(lostSoul2);

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
            world.DataCache.CacheEntities = false;
            var lostSoul1 = GameActions.CreateEntity(world, "LostSoul", new Vec3D(-256, -64, 0));
            var caco1 = GameActions.CreateEntity(world, "Cacodemon", new Vec3D(-256, -64, 0));

            lostSoul1.SetTarget(caco1);
            caco1.SetTarget(lostSoul1);

            lostSoul1.Kill(null);
            GameActions.TickWorld(world, 200);

            caco1.Target.Entity.Should().BeNull();
        }

        //[Fact(DisplayName = "Dispose ref")]
        //public void DisposeTest()
        //{
        //    var world = WorldAllocator.LoadMap(Resource, File, "MAP01", Guid.NewGuid().ToString(), WorldInit, IWadType.Doom2);
        //    world.DataCache.CacheEntities = false;
        //    TestDisposeEntities(world, out var lostSoul, out var caco);

        //    GC.Collect();

        //    // This test makes sure that there are no dangling references to the entities that prevents the GC from collecting.
        //    lostSoul.TryGetTarget(out var target).Should().BeFalse();
        //    caco.TryGetTarget(out target).Should().BeFalse();
        //}

        private static void TestDisposeEntities(WorldBase world, out WeakReference<Entity> lostSoul, out WeakReference<Entity> caco)
        {
            Entity lostSoul1 = world.EntityManager.Create( "LostSoul", new Vec3D(-256, -64, 0))!;
            Entity caco1 = world.EntityManager.Create("Cacodemon", new Vec3D(-256, -64, 0))!;
            Entity caco2 = world.EntityManager.Create("Cacodemon", new Vec3D(-256, -64, 0))!;

            lostSoul1.SetTarget(caco1);
            caco1.SetTarget(lostSoul1);
            caco1.SetTracer(caco2);
            caco2.SetTracer(caco1);

            lostSoul = new WeakReference<Entity>(lostSoul1);
            caco = new WeakReference<Entity>(caco1);

            lostSoul1.Dispose();
            caco1.Dispose();
        }
    }
}
