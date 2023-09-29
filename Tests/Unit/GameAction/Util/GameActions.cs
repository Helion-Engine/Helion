using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;
using Helion.World.Physics.Blockmap;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Tests.Unit.GameAction
{
    public enum Bearing
    {
        East,
        North,
        West,
        South,
    }

    public static partial class GameActions
    {
        public static Line GetLine(WorldBase world, int id) => world.Lines.First(x => x.Id == id);

        public static Sector GetSector(WorldBase world, int id) => world.Sectors.First(x => x.Id == id);

        public static Sector GetSectorByTag(WorldBase world, int tag) => world.Sectors.First(x => x.Tag == tag);

        public static Entity GetEntity(WorldBase world, int id)
        {
            var node = world.Entities.Head;
            while (node != null)
            {
                if (node.Value.Id == id)
                    return node.Value;

                node = node.Next;
            }

            throw new NullReferenceException();
        }

        public static Entity GetEntity(WorldBase world, string name)
        {
            var node = world.Entities.Head;
            while (node != null)
            {
                if (node.Value.Definition.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return node.Value;

                node = node.Next;
            }

            throw new NullReferenceException();
        }

        public static List<Entity> GetEntities(WorldBase world, string name)
        {
            List<Entity> entities = new();
            var node = world.Entities.Head;
            while (node != null)
            {
                if (node.Value.Definition.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    entities.Add(node.Value);

                node = node.Next;
            }

            return entities;
        }

        public static Entity GetSectorEntity(WorldBase world, int sectorId, string type)
        {
            var entities = GetSectorEntities(world, sectorId, type);
            entities.Count.Should().Be(1);
            return entities[0];
        }

        public static List<Entity> GetSectorEntities(WorldBase world, int sectorId, string? type = null)
        {
            List<Entity> entities = new();
            var sector = GetSector(world, sectorId);

            var node = sector.Entities.Head;
            while (node != null)
            {
                if (string.IsNullOrEmpty(type) || node.Value.Definition.Name.Equals(type, StringComparison.OrdinalIgnoreCase))
                    entities.Add(node.Value);
                node = node.Next;
            }

            return entities;
        }

        public static readonly List<Entity> CreatedEntities = new();

        public static Entity CreateEntity(WorldBase world, string name, Vec3D pos, bool frozen = true, Action<Entity>? onCreated = null, bool init = false)
        {
            var createdEntity = world.EntityManager.Create(name, pos, init: init);
            createdEntity.Should().NotBeNull();
            if (frozen)
                createdEntity!.FrozenTics = int.MaxValue;
            CreatedEntities.Add(createdEntity!);
            onCreated?.Invoke(createdEntity!);
            return createdEntity!;
        }

        public static EntityDefinition GetEntityDefinition(WorldBase world, string name)
        {
            var def = world.EntityManager.DefinitionComposer.GetByName(name);
            def.Should().NotBeNull();
            return def!;
        }

        public static void DestroyCreatedEntities(WorldBase world)
        {
            foreach (var entity in CreatedEntities)
            {
                if (entity.IsDisposed)
                    continue;

                world.EntityManager.Destroy(entity);
            }

            CreatedEntities.Clear();
        }

        public static void DestroyEntities(WorldBase world, string name)
        {
            List<Entity> destroyEntities = new();
            var node = world.EntityManager.Entities.Head;

            while (node != null)
            {
                if (node.Value.Definition.Name.EqualsIgnoreCase(name))
                    destroyEntities.Add(node.Value);
                node = node.Next;
            }

            destroyEntities.ForEach(x => world.EntityManager.Destroy(x));
        }

        public static bool SetEntityToLine(WorldBase world, Entity entity, int lineId, double distanceBeforeLine)
        {
            var line = GetLine(world, lineId);
            if (line == null)
                return false;

            Vec2D center = line.Segment.FromTime(0.5);

            double angle = line.Segment.Start.Angle(line.Segment.End);
            angle -= MathHelper.HalfPi;

            center += distanceBeforeLine * Vec2D.UnitCircle(angle);

            entity.Position = center.To3D(entity.Position.Z);
            entity.UnlinkFromWorld();
            world.Link(entity);
            entity.CheckOnGround();
            entity.AngleRadians = angle + Math.PI;

            return true;
        }

        // forceActivation will ignore if the line was previously activated. Required for testing different scenarios on single use specials.
        public static bool EntityCrossLine(WorldBase world, Entity entity, int lineId, bool forceActivation = false, bool moveOutofBounds = true,
            bool forceFrozen = true)
        {
            if (!SetEntityToLine(world, entity, lineId, entity.Radius))
                return false;

            if (forceActivation)
            {
                Line line = GetLine(world, lineId);
                line.SetActivated(false);
            }

            entity.FrozenTics = 0;
            MoveEntity(world, entity, entity.Radius * 2);

            if (forceFrozen)
                entity.FrozenTics = int.MaxValue;

            if (moveOutofBounds)
                SetEntityOutOfBounds(world, entity);

            return true;
        }

        public static bool EntityBlockedByLine(WorldBase world, Entity entity, int lineId)
        {
            if (!SetEntityToLine(world, entity, lineId, entity.Radius))
                throw new Exception("Line not found");

            var pos = entity.Position;
            entity.FrozenTics = 0;
            MoveEntity(world, entity, entity.Radius * 2);
            var nextPos = entity.Position;

            SetEntityOutOfBounds(world, entity);

            return pos == nextPos;
        }

        public static bool EntityUseLine(WorldBase world, Entity entity, int lineId)
        {
            if (!SetEntityToLine(world, entity, lineId, entity.Radius))
                return false;

            bool success = world.EntityUse(entity);
            SetEntityOutOfBounds(world, entity);
            return success;
        }

        // Activates the line given the context. Will force even if not repeatable.
        public static bool ActivateLine(WorldBase world, Entity entity, int lineId, ActivationContext context)
        {
            Line line = GetLine(world, lineId);
            line.SetActivated(false);
            return world.ActivateSpecialLine(entity, line, context);
        }

        public static bool PlayerFirePistol(WorldBase world, Player player)
        {
            var pistol = player.Inventory.Weapons.GetWeapon("PISTOL");
            if (pistol == null)
                return false;

            player.ChangeWeapon(pistol);
            if (player.Inventory.Amount("CLIP") <= 0)
                player.Inventory.Add("CLIP", 100);

            TickWorld(world, () =>
            {
                if (player.PendingWeapon != null)
                    return true;
                if (player.Weapon == null)
                    return true;
                if (!player.Weapon.ReadyToFire)
                    return true;
                return false;
            }, () => { });

            player.FireWeapon();
            world.Tick();

            TickWorld(world, () =>
            {
                return player.Weapon != null && !player.Weapon.ReadyToFire;
            }, () => { });

            return true;
        }

        public static BlockmapIntersect? FireHitscanTest(WorldBase world, Entity entity)
        {
            Vec3D intersect = Vec3D.Zero;
            double angle = entity.AngleRadians;
            double pitch = entity.PlayerObj == null ? 0 : entity.PlayerObj.PitchRadians;
            return world.FireHitScan(entity, entity.HitscanAttackPos, Vec3D.UnitSphere(entity.AngleRadians, 0) * Constants.EntityShootDistance, 
                angle, pitch, Constants.EntityShootDistance, 5, HitScanOptions.Default, ref intersect, out _);
        }

        public static void SetEntityOutOfBounds(WorldBase world, Entity entity)
        {
            // Move them very far away to not mess with physics
            Vec3D pos = new(16000, 16000, 0);
            if (entity.IsPlayer)
                pos = new(-16000, -16000, 0);

            entity.SetSpawnState();

            entity.UnlinkFromWorld();
            entity.Position = pos;
            world.Link(entity);
        }

        public static void MoveEntity(WorldBase world, Entity entity, double distance)
        {
            Vec3D velocity = distance * Vec3D.UnitSphere(entity.AngleRadians, 0);
            entity.Velocity = velocity;
            world.Tick();
            entity.Velocity = Vec3D.Zero;
        }

        public static void MoveEntity(WorldBase world, Entity entity, Vec2D pos)
        {
            double distance = entity.Position.XY.Distance(pos);
            entity.AngleRadians = entity.Position.XY.Angle(pos);
            MoveEntity(world, entity, distance);
        }

        public static void TickWorld(WorldBase world, int ticks)
        {
            for (int i = 0; i < ticks; i++)
                world.Tick();
        }

        public static void TickWorld(WorldBase world, int ticks, Action action)
        {
            for (int i = 0; i < ticks; i++)
            {
                world.Tick();
                action();
            }
        }

        public static void TickWorld(WorldBase world, Func<bool> runWhile, Action action, TimeSpan? timeout = null)
        {
            if (!timeout.HasValue)
                timeout = TimeSpan.FromSeconds(60);

            int runTicks = 0;
            while (true)
            {
                world.Tick();
                runTicks++;
                if (!runWhile())
                    break;

                if (runTicks > 35 * timeout.Value.TotalSeconds)
                    throw new Exception($"Tick world ran for more than {timeout.Value.TotalSeconds} seconds");

                action();
            }
        }

        public static void CheckMonsterUseActivation(WorldBase world, Entity monster, int lineId, Sector sector, SectorPlane plane, bool canActivate)
        {
            Line line = GetLine(world, lineId);
            line.SetActivated(false);
            EntityUseLine(world, monster, lineId);

            if (canActivate)
                sector.GetActiveMoveSpecial(plane).Should().NotBeNull();
            else
                sector.GetActiveMoveSpecial(plane).Should().BeNull();
        }

        public static void CheckMonsterCrossActivation(WorldBase world, Entity monster, int lineId, Sector sector, SectorPlane plane, bool canActivate)
        {
            Line line = GetLine(world, lineId);
            line.SetActivated(false);
            EntityCrossLine(world, monster, lineId).Should().BeTrue();

            if (canActivate)
                sector.GetActiveMoveSpecial(plane).Should().NotBeNull();
            else
                sector.GetActiveMoveSpecial(plane).Should().BeNull();
        }

        public static void CheckNoReactivateEntityCross(WorldBase world, Entity entity, int lineId, Sector sector, SectorPlane plane)
        {
            EntityCrossLine(world, entity, lineId).Should().BeTrue();
            sector.GetActiveMoveSpecial(plane).Should().BeNull();
        }

        public static void CheckNoReactivateEntityUse(WorldBase world, Entity entity, int lineId, Sector sector, SectorPlane plane)
        {
            EntityUseLine(world, entity, lineId).Should().BeFalse();
            sector.GetActiveMoveSpecial(plane).Should().BeNull();
        }

        public static void CheckPlaneTexture(WorldBase world, SectorPlane plane, string name)
        {
            world.TextureManager.GetTexture(plane.TextureHandle).Name.Equals(name, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        }

        public static void CheckSideTexture(WorldBase world, Side side, WallLocation location, string name)
        {
            Wall wall = location switch
            {
                WallLocation.Upper => side.Upper,
                WallLocation.Lower => side.Lower,
                _ => side.Middle,
            };
            world.TextureManager.GetTexture(wall.TextureHandle).Name.Equals(name, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        }

        public static void ForceStopSectorSpecial(WorldBase world, Sector sector)
        {
            var special = world.SpecialManager.FindSpecialBySector(sector);
            special.Should().NotBeNull();
            world.SpecialManager.RemoveSpecial(special!).Should().BeTrue();
        }

        public static bool GiveItem(Player player, string item)
        {
            var def = player.World.EntityManager.DefinitionComposer.GetByName(item);
            if (def == null)
                return false;

            return player.GiveItem(def, null);
        }

        public static void RemoveItem(Player player, string item)
        {
            player.Inventory.Remove(item, 1);
        }

        public static double MoveZ(double z, double move, double dest)
        {
            if (move < 0)
                return Math.Clamp(z + move, dest, double.MaxValue);

            return Math.Clamp(z + move, double.MinValue, dest);
        }

        public static void SetEntityPosition(WorldBase world, Entity entity, Vec2D pos) =>
            SetEntityPosition(world, entity, pos.To3D(0));

        public static void SetEntityPosition(WorldBase world, Entity entity, Vec3D pos)
        {
            entity.UnlinkFromWorld();
            entity.PrevPosition = pos;
            entity.Position = pos;
            world.LinkClamped(entity);
        }

        public static void SetEntityPositionInit(WorldBase world, Entity entity, Vec3D pos)
        {
            entity.UnlinkFromWorld();
            entity.MoveLinked = false;
            entity.PrevPosition = pos;
            entity.Position = pos;
            world.Link(entity);
        }

        public static void SetEntityTarget(Entity source, Entity target)
        {
            source.SetTarget(target);
            source.SetSeeState();
        }

        public static double GetAngle(Bearing bearing) => MathHelper.HalfPi * (int)bearing;
    }
}
