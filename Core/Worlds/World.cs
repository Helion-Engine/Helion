﻿using System;
using System.Collections.Generic;
using Helion.Audio;
using Helion.Maps;
using Helion.Maps.Specials.Compatibility;
using Helion.Resource;
using Helion.Resource.Definitions.Decorate.Locks;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Container.Linkable;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Segments;
using Helion.Util.Geometry.Vectors;
using Helion.Util.RandomGenerators;
using Helion.Util.Time;
using Helion.Worlds.Blockmap;
using Helion.Worlds.Bsp;
using Helion.Worlds.Entities;
using Helion.Worlds.Entities.Players;
using Helion.Worlds.Geometry;
using Helion.Worlds.Geometry.Builder;
using Helion.Worlds.Geometry.Lines;
using Helion.Worlds.Geometry.Sectors;
using Helion.Worlds.Geometry.Walls;
using Helion.Worlds.Physics;
using Helion.Worlds.Physics.Blockmap;
using Helion.Worlds.Sound;
using Helion.Worlds.Special;
using Helion.Worlds.Special.SectorMovement;
using Helion.Worlds.Textures;
using MoreLinq;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Worlds
{
    public abstract class World
    {
        private const double MaxPitch = 80.0 * Math.PI / 180.0;
        private const double MinPitch = -80.0 * Math.PI / 180.0;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Fires when an entity activates a line special with use or by crossing a line.
        /// </summary>
        public event EventHandler<EntityActivateSpecialEventArgs>? EntityActivatedSpecial;
        public event EventHandler<LevelChangeEvent>? LevelExit;

        public readonly long CreationTimeNanos = Ticker.NanoTime();
        public readonly CIString MapName;
        public readonly BlockMap Blockmap;
        public Config Config { get; }
        public WorldState WorldState { get; protected set; } = WorldState.Normal;
        public int Gametick { get; private set; }
        public int LevelTime { get; private set; }
        public double Gravity { get; private set; } = 1.0;
        public IRandom Random => m_random;
        protected readonly Resources Resources;
        protected readonly MapGeometry Geometry;
        protected readonly SpecialManager SpecialManager;
        protected readonly PhysicsManager PhysicsManager;
        protected readonly WorldTextureManager TextureManager;
        private int m_exitTicks;
        private LevelChangeType m_levelChangeType = LevelChangeType.Next;

        public IList<Line> Lines => Geometry.Lines;
        public IList<Side> Sides => Geometry.Sides;
        public IList<Wall> Walls => Geometry.Walls;
        public IList<Sector> Sectors => Geometry.Sectors;
        public BspTree BspTree => Geometry.BspTree;
        public LinkableList<Entity> Entities => EntityManager.Entities;
        public EntityManager EntityManager { get; }
        public SoundManager SoundManager { get; }
        public abstract Vec3D ListenerPosition { get; }
        public abstract double ListenerAngle { get; }
        public abstract double ListenerPitch { get; }
        public abstract Entity ListenerEntity { get; }
        public BlockmapTraverser BlockmapTraverser => PhysicsManager.BlockmapTraverser;
        private readonly DoomRandom m_random = new();
        private int m_soundCount;

        public World(Config config, Resources resources, IAudioSystem audioSystem, Map map)
        {
            Resources = resources;
            Config = config;
            MapName = map.Name;
            TextureManager = new(resources);
            Geometry = CreateMapGeometry(map);
            Blockmap = new BlockMap(Lines);
            SoundManager = new SoundManager(this, audioSystem, resources.Sounds);
            EntityManager = new EntityManager(this, resources, SoundManager, config.Engine.Game.Skill);
            PhysicsManager = new PhysicsManager(this, BspTree, Blockmap, SoundManager, EntityManager, m_random);
            SpecialManager = new SpecialManager(this, TextureManager, resources, m_random);
        }

        private MapGeometry CreateMapGeometry(Map map)
        {
            MapGeometry? geometry = GeometryBuilder.Create(map, TextureManager);
            if (geometry != null)
                return geometry;

            Log.Error("Map geometry is corrupt, cannot create map {0}", map.Name);
            throw new Exception($"Map geometry is corrupt, cannot create map {map.Name}");
        }

        ~World()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public Player? GetLineOfSightPlayer(Entity entity, bool allaround)
        {
            for (int i = 0; i < EntityManager.Players.Count; i++)
            {
                Player player = EntityManager.Players[i];

                if (!allaround)
                {
                    Vec2D entityLookingVector = Vec2D.RadiansToUnit(entity.AngleRadians);
                    Vec2D entityToTarget = player.Position.To2D() - entity.Position.To2D();

                    // Not in front 180 FOV or MeleeRange
                    if (entityToTarget.Dot(entityLookingVector) < 0 &&
                        entity.Position.ApproximateDistance2D(player.Position) > Constants.EntityMeleeDistance)
                        continue;
                }

                if (!player.IsDead && CheckLineOfSight(entity, player))
                    return player;
            }

            return null;
        }

        public void NoiseAlert(Entity target)
        {
            m_soundCount++;
            RecursiveSound(target, target.Sector, 0);
        }

        public void RecursiveSound(Entity target, Sector sector, int block)
        {
            if (sector.SoundValidationCount == m_soundCount && sector.SoundBlock <= block + 1)
                return;

            sector.SoundValidationCount = m_soundCount;
            sector.SoundBlock = block + 1;
            sector.SoundTarget = target;

            for (int i = 0; i < sector.Lines.Count; i++)
            {
                Line line = sector.Lines[i];
                if (line.Back == null || !LineOpening.IsOpen(line))
                    continue;

                Sector other = ReferenceEquals(line.Front.Sector, sector) ? line.Back.Sector : line.Front.Sector;
                if (line.Flags.BlockSound)
                {
                    // Has to cross two block sound lines to stop. This is how it was designed.
                    if (block == 0)
                        RecursiveSound(target, other, 1);
                }
                else
                {
                    RecursiveSound(target, other, block);
                }
            }
        }

        public void Link(Entity entity)
        {
            Precondition(entity.SectorNodes.Empty() && entity.BlockmapNodes.Empty(), "Forgot to unlink entity before linking");

            PhysicsManager.LinkToWorld(entity, null, false);
        }

        public void Tick()
        {
            if (WorldState == WorldState.Exit)
            {
                m_exitTicks--;
                if (m_exitTicks == 0)
                    LevelExit?.Invoke(this, new LevelChangeEvent(m_levelChangeType));
            }
            else if (WorldState == WorldState.Normal)
            {
                EntityManager.Entities.ForEach(entity =>
                {
                    entity.Tick();

                    // Entities can be disposed after Tick() (rocket explosion, blood spatter etc.)
                    if (!entity.IsDisposed)
                        PhysicsManager.Move(entity);
                });

                foreach (Player player in EntityManager.Players)
                    player.Sector.SectorDamageSpecial?.Tick(player);

                SpecialManager.Tick();
                TextureManager.Tick();

                LevelTime++;
            }

            SoundManager.Tick();

            Gametick++;
        }

        public IEnumerable<Sector> FindBySectorTag(int tag)
        {
            return Geometry.FindBySectorTag(tag);
        }

        public IEnumerable<Entity> FindByTid(int tid)
        {
            return EntityManager.FindByTid(tid);
        }

        public void Dispose()
        {
            PerformDispose();
            GC.SuppressFinalize(this);
        }

        public void ExitLevel(LevelChangeType type)
        {
            SoundManager.ClearSounds();
            m_levelChangeType = type;
            WorldState = WorldState.Exit;
            m_exitTicks = 15;

            foreach (Player player in EntityManager.Players)
                player.ResetInterpolation();
        }

        public List<Entity> GetBossTargets()
        {
            List<Entity> targets = new();
            EntityManager.Entities.ForEach(entity =>
            {
                if (entity.Definition.Name == "BOSSTARGET")
                    targets.Add(entity);
            });

            return targets;
        }

        public int CurrentBossTarget { get; set; }

        public void TelefragBlockingEntities(Entity entity)
        {
            List<Entity> blockingEntities = entity.GetIntersectingEntities3D(entity.Position, BlockmapTraverseEntityFlags.Solid | BlockmapTraverseEntityFlags.Shootable);
            for (int i = 0; i < blockingEntities.Count; i++)
                blockingEntities[i].ForceGib();
        }

        /// <summary>
        /// Executes use logic on the entity. EntityUseActivated event will
        /// fire if the entity activates a line special or is in range to hit
        /// a blocking line. PlayerUseFail will fire if the entity is a player
        /// and we hit a block line but didn't activate a special.
        /// </summary>
        /// <remarks>
        /// If the line has a special and we are hitting the front then we
        /// can use it (player Z does not apply here). If there's a LineOpening
        /// with OpeningHeight less than or equal to 0, it's a closed sector.
        /// The special line behind it cannot activate until the sector has an
        /// opening.
        /// </remarks>
        /// <param name="entity">The entity to execute use.</param>
        public virtual bool EntityUse(Entity entity)
        {
            if (entity.IsDead)
                return false;

            Line? activateLine = null;
            bool hitBlockLine = false;
            Vec2D start = entity.Position.To2D();
            Vec2D end = start + (Vec2D.RadiansToUnit(entity.AngleRadians) * entity.Properties.Player.UseRange);
            List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(new Seg2D(start, end), BlockmapTraverseFlags.Lines);

            for (int i = 0; i < intersections.Count; i++)
            {
                BlockmapIntersect bi = intersections[i];
                if (bi.Line != null)
                {
                    if (bi.Line.Segment.OnRight(start))
                    {
                        if (bi.Line.HasSpecial)
                        {
                            activateLine = bi.Line;
                            break;
                        }

                        if (bi.Line.Back == null)
                        {
                            hitBlockLine = true;
                            break;
                        }
                    }

                    if (bi.Line.Back != null)
                    {
                        LineOpening opening = PhysicsManager.GetLineOpening(bi.Intersection, bi.Line);
                        if (opening.OpeningHeight <= 0)
                        {
                            hitBlockLine = true;
                            break;
                        }

                        // Keep checking if hit two-sided blocking line - this way the PlayerUserFail will be raised if no line special is hit
                        if (!opening.CanPassOrStepThrough(entity))
                            hitBlockLine = true;
                    }
                }
            }

            bool activateSuccess = activateLine != null && ActivateSpecialLine(entity, activateLine, ActivationContext.UseLine);
            if (!activateSuccess && hitBlockLine && entity is Player player)
                player.PlayUseFailSound();

            return activateSuccess;
        }

        public bool CanActivate(Entity entity, Line line, ActivationContext context)
        {
            bool success = line.Special.CanActivate(entity, line, context, Resources.Locks, out LockDef? lockFail);
            if (entity is Player player && lockFail != null)
            {
                player.PlayUseFailSound();
                DisplayMessage(player, GetLockFailMessage(line, lockFail));
            }
            return success;
        }

        private string GetLockFailMessage(Line line, LockDef lockDef)
        {
            if (line.Special.LineSpecialCompatibility != null &&
                line.Special.LineSpecialCompatibility.CompatibilityType == LineSpecialCompatibilityType.KeyObject)
                return $"You need a {lockDef.Message} to activate this object.";
            else
                return $"You need a {lockDef.Message} to open this door.";
        }

        /// <summary>
        /// Attempts to activate a line special given the entity, line, and context.
        /// </summary>
        /// <remarks>
        /// Does not do any range checking. Only verifies if the entity can activate the line special in this context.
        /// </remarks>
        /// <param name="entity">The entity to execute special.</param>
        /// <param name="line">The line containing the special to execute.</param>
        /// <param name="context">The ActivationContext to attempt to execute the special.</param>
        public virtual bool ActivateSpecialLine(Entity entity, Line line, ActivationContext context)
        {
            if (!CanActivate(entity, line, context))
                return false;

            EntityActivateSpecialEventArgs args = new(context, entity, line);
            EntityActivatedSpecial?.Invoke(this, args);
            return true;
        }

        public bool GetAutoAimEntity(Entity startEntity, in Vec3D start, double angle, double distance, out double pitch, out Entity? entity)
        {
            Vec3D end = start + Vec3D.UnitTimesValue(angle, 0, distance);
            return GetAutoAimAngle(startEntity, start, end, out pitch, out entity);
        }

        public virtual Entity? FireProjectile(Entity shooter, double pitch, double distance, bool autoAim, string projectClassName, double zOffset = 0.0)
        {
            if (shooter is Player player)
                player.DescreaseAmmo();

            Vec3D start = shooter.ProjectileAttackPos;
            start.Z += zOffset;

            if (autoAim)
            {
                Vec3D end = start + Vec3D.UnitTimesValue(shooter.AngleRadians, pitch, distance);
                if (GetAutoAimAngle(shooter, start, end, out double autoAimPitch, out _))
                    pitch = autoAimPitch;
            }

            var projectileDef = EntityManager.DefinitionComposer.GetByName(projectClassName);
            if (projectileDef != null)
            {
                Entity projectile = EntityManager.Create(projectileDef, start, 0.0, shooter.AngleRadians, 0);
                Vec3D velocity = Vec3D.UnitTimesValue(shooter.AngleRadians, pitch, projectile.Definition.Properties.Speed);
                Vec3D testPos = projectile.Position + Vec3D.UnitTimesValue(shooter.AngleRadians, pitch, shooter.Radius - 2.0);
                projectile.Owner = shooter;
                projectile.PlaySeeSound();

                // TryMoveXY will use the velocity of the projectile
                // A projectile spawned where it can't fit can cause BlockingSectorPlane or BlockingEntity (IsBlocked = true)
                if (projectile.Flags.NoClip || (!projectile.IsBlocked() && PhysicsManager.TryMoveXY(projectile, testPos.To2D(), true).Success))
                {
                    projectile.Velocity = velocity;
                    return projectile;
                }

                projectile.SetPosition(testPos);
                HandleEntityHit(projectile, velocity, null);
            }

            return null;
        }

        public virtual void FireHitscanBullets(Entity shooter, int bulletCount, double spreadAngleRadians, double spreadPitchRadians, double pitch, double distance, bool autoAim)
        {
            if (shooter is Player player)
                player.DescreaseAmmo();

            if (autoAim)
            {
                Vec3D start = shooter.HitscanAttackPos;
                Vec3D end = start + Vec3D.UnitTimesValue(shooter.AngleRadians, pitch, distance);
                if (GetAutoAimAngle(shooter, start, end, out double autoAimPitch, out _))
                    pitch = autoAimPitch;
            }

            if (!shooter.Refire && bulletCount == 1)
            {
                int damage = 5 * ((m_random.NextByte() % 3) + 1);
                FireHitscan(shooter, shooter.AngleRadians, pitch, distance, damage);
            }
            else
            {
                for (int i = 0; i < bulletCount; i++)
                {
                    int damage = 5 * ((m_random.NextByte() % 3) + 1);
                    double angle = shooter.AngleRadians + (m_random.NextDiff() * spreadAngleRadians / 255);
                    double newPitch = pitch + (m_random.NextDiff() * spreadPitchRadians / 255);
                    FireHitscan(shooter, angle, newPitch, distance, damage);
                }
            }
        }

        public virtual Entity? FireHitscan(Entity shooter, double angle, double pitch, double distance, int damage)
        {
            Vec3D start = shooter.HitscanAttackPos;
            Vec3D end = start + Vec3D.UnitTimesValue(angle, pitch, distance);
            Vec3D intersect = new Vec3D(0, 0, 0);

            BlockmapIntersect? bi = FireHitScan(shooter, start, end, pitch, ref intersect);

            if (bi != null)
            {
                Line? line = bi.Value.Line;
                if (line != null && line.HasSpecial && CanActivate(shooter, line, ActivationContext.ProjectileHitLine))
                {
                    var args = new EntityActivateSpecialEventArgs(ActivationContext.ProjectileHitLine, shooter, line);
                    EntityActivatedSpecial?.Invoke(this, args);
                }

                if (damage > 0)
                {
                    // Only move closer on a line hit
                    if (bi.Value.Entity == null && bi.Value.Sector == null)
                        MoveIntersectCloser(start, ref intersect, angle, bi.Value.Distance2D);
                    DebugHitscanTest(bi.Value, intersect);
                }

                if (bi.Value.Entity != null)
                {
                    DamageEntity(bi.Value.Entity, shooter, damage, Thrust.Horizontal);
                    return bi.Value.Entity;
                }
            }

            return null;
        }

        public virtual BlockmapIntersect? FireHitScan(Entity shooter, Vec3D start, Vec3D end, double pitch, ref Vec3D intersect)
        {
            double floorZ, ceilingZ;
            Seg2D seg = new Seg2D(start.To2D(), end.To2D());
            List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(seg,
                BlockmapTraverseFlags.Entities | BlockmapTraverseFlags.Lines,
                BlockmapTraverseEntityFlags.Shootable | BlockmapTraverseEntityFlags.Solid);

            for (int i = 0; i < intersections.Count; i++)
            {
                BlockmapIntersect bi = intersections[i];

                if (bi.Line != null)
                {
                    intersect = bi.Intersection.To3D(start.Z + (Math.Tan(pitch) * bi.Distance2D));

                    if (bi.Line.Back == null)
                    {
                        floorZ = bi.Line.Front.Sector.ToFloorZ(intersect);
                        ceilingZ = bi.Line.Front.Sector.ToCeilingZ(intersect);

                        if (intersect.Z > floorZ && intersect.Z < ceilingZ)
                            return bi;

                        if (IsSkyClipOneSided(bi.Line.Front.Sector, floorZ, ceilingZ, intersect))
                            return null;

                        GetSectorPlaneIntersection(start, end, bi.Line.Front.Sector, floorZ, ceilingZ, ref intersect);
                        bi.Sector = bi.Line.Front.Sector;
                        return bi;
                    }

                    GetOrderedSectors(bi.Line, start, out Sector front, out Sector back);
                    if (IsSkyClipTwoSided(front, back, intersect))
                        return null;

                    floorZ = front.ToFloorZ(intersect);
                    ceilingZ = front.ToCeilingZ(intersect);

                    if (intersect.Z < floorZ || intersect.Z > ceilingZ)
                    {
                        GetSectorPlaneIntersection(start, end, front, floorZ, ceilingZ, ref intersect);
                        bi.Sector = front;
                        return bi;
                    }

                    LineOpening opening = PhysicsManager.GetLineOpening(bi.Intersection, bi.Line);
                    if ((opening.FloorZ > intersect.Z && intersect.Z > floorZ) || (opening.CeilingZ < intersect.Z && intersect.Z < ceilingZ))
                        return bi;
                }
                else if (bi.Entity != null && !ReferenceEquals(shooter, bi.Entity) && bi.Entity.Box.Intersects(start, end, ref intersect))
                {
                    return bi;
                }
            }

            return null;
        }

        public virtual bool DamageEntity(Entity target, Entity? source, int damage, Thrust thrust = Thrust.HorizontalAndVertical)
        {
            if (!target.Flags.Shootable || damage == 0)
                return false;

            Vec3D thrustVelocity = Vec3D.Zero;

            if (source != null && thrust != Thrust.None)
            {
                Vec2D xyDiff = source.Position.To2D() - target.Position.To2D();
                bool zEqual = Math.Abs(target.Position.Z - source.Position.Z) <= double.Epsilon;
                bool xyEqual = Math.Abs(xyDiff.X) <= 1.0 && Math.Abs(xyDiff.Y) <= 1.0;
                double pitch = 0.0;

                double angle = source.Position.Angle(target.Position);
                double thrustAmount = damage * source.Definition.Properties.ProjectileKickBack * 0.125 / target.Properties.Mass;

                // Silly vanilla doom feature that allows target to be thrown forward sometimes
                if (damage < 40 && damage > target.Health &&
                    target.Position.Z - source.Position.Z > 64 && (m_random.NextByte() & 1) != 0)
                {
                    angle += Math.PI;
                    thrustAmount *= 4;
                }

                if (thrust == Thrust.HorizontalAndVertical)
                {
                    // Player rocket jumping check, back up the source Z to get a valid pitch
                    // Only done for players, otherwise blowing up enemies will launch them in the air
                    if (zEqual && target is Player && source.Owner == target)
                    {
                        Vec3D sourcePos = new Vec3D(source.Position.X, source.Position.Y, source.Position.Z - 1.0);
                        pitch = sourcePos.Pitch(target.Position, 0.0);
                    }
                    else if (source.Position.Z < target.Position.Z || source.Position.Z > target.Position.Z + target.Height)
                    {
                        Vec3D sourcePos = source.CenterPoint;
                        Vec3D targetPos = target.Position;
                        if (source.Position.Z > target.Position.Z + target.Height)
                            targetPos.Z += target.Height;
                        pitch = sourcePos.Pitch(targetPos, sourcePos.To2D().Distance(targetPos.To2D()));
                    }

                    if (!xyEqual)
                        thrustVelocity = Vec3D.Unit(angle, 0.0);

                    thrustVelocity.Z = Math.Sin(pitch);
                }
                else
                {
                    thrustVelocity = Vec3D.Unit(angle, 0.0);
                }

                thrustVelocity.Multiply(thrustAmount);
            }

            if (target.Damage(source, damage, m_random.NextByte() < target.Properties.PainChance) || (target is Player && target.Flags.Invulnerable))
                target.Velocity += thrustVelocity;

            if (target.IsDead)
                HandleEntityDeath(target);

            return true;
        }

        public virtual void HandleEntityHit(Entity entity, in Vec3D previousVelocity, TryMoveData? tryMove)
        {
            entity.Hit(previousVelocity);

            if (entity.Flags.Missile)
            {
                if (tryMove != null)
                {
                    for (int i = 0; i < tryMove.IntersectSpecialLines.Count; i++)
                        ActivateSpecialLine(entity, tryMove.IntersectSpecialLines[i], ActivationContext.ProjectileHitLine);
                }

                if (entity.BlockingEntity != null)
                {
                    int damage = entity.Properties.Damage.Get(m_random);
                    DamageEntity(entity.BlockingEntity, entity, damage);
                }

                bool skyClip = false;

                if (entity.BlockingLine != null)
                {
                    if (entity.BlockingLine.OneSided && IsSkyClipOneSided(entity.BlockingLine.Front.Sector, entity.BlockingLine.Front.Sector.ToFloorZ(entity.Position),
                        entity.BlockingLine.Front.Sector.ToCeilingZ(entity.Position), entity.Position))
                    {
                        skyClip = true;
                    }
                    else if (!entity.BlockingLine.OneSided)
                    {
                        GetOrderedSectors(entity.BlockingLine, entity.Position, out Sector front, out Sector back);
                        if (IsSkyClipTwoSided(front, back, entity.Position))
                            skyClip = true;
                    }
                }

                if (entity.BlockingSectorPlane != null && entity.BlockingSectorPlane.Texture.IsSky)
                    skyClip = true;

                if (skyClip)
                    EntityManager.Destroy(entity);
                else
                    entity.SetDeathState(null);

                HandleEntityDeath(entity);
            }
            else if (tryMove != null && entity is Player)
            {
                for (int i = 0; i < tryMove.IntersectSpecialLines.Count; i++)
                    ActivateSpecialLine(entity, tryMove.IntersectSpecialLines[i], ActivationContext.PlayerPushesWall);
            }
        }

        public virtual bool CheckLineOfSight(Entity from, Entity to)
        {
            Vec2D start = from.Position.To2D();
            Vec2D end = to.Position.To2D();

            if (start == end)
                return true;

            Seg2D seg = new Seg2D(start, end);

            List<BlockmapIntersect> intersections = BlockmapTraverser.Traverse(null, seg, BlockmapTraverseFlags.Lines | BlockmapTraverseFlags.StopOnOneSidedLine,
                BlockmapTraverseEntityFlags.None, out bool hitOneSidedLine);
            if (hitOneSidedLine)
                return false;

            Vec3D sightPos = new Vec3D(from.Position.X, from.Position.Y, from.Position.Z + (from.Height * 0.75));
            double distance2D = start.Distance(end);
            double topPitch = sightPos.Pitch(to.Position.Z + to.Height, distance2D);
            double bottomPitch = sightPos.Pitch(to.Position.Z, distance2D);


            return GetBlockmapTraversalPitch(intersections, sightPos, from, topPitch, bottomPitch, out _, out _) != TraversalPitchStatus.Blocked;
        }

        public virtual void RadiusExplosion(Entity source, int radius)
        {
            // Barrels do not apply Z thrust - TODO better way to check?
            Thrust thrust = source.Definition.Name == "ExplosiveBarrel" ? Thrust.Horizontal : Thrust.HorizontalAndVertical;
            Vec2D pos2D = source.Position.To2D();
            Vec2D radius2D = new Vec2D(radius, radius);
            Box2D explosionBox = new Box2D(pos2D - radius2D, pos2D + radius2D);

            List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(explosionBox, BlockmapTraverseFlags.Entities,
                BlockmapTraverseEntityFlags.Shootable | BlockmapTraverseEntityFlags.Solid);
            for (int i = 0; i < intersections.Count; i++)
            {
                BlockmapIntersect bi = intersections[i];
                if (bi.Entity != null && !bi.Entity.Flags.NoRadiusDmg && CheckLineOfSight(bi.Entity, source))
                    ApplyExplosionDamageAndThrust(source, bi.Entity, radius, thrust);
            }
        }

        public virtual TryMoveData TryMoveXY(Entity entity, Vec2D position, bool stepMove = true)
            => PhysicsManager.TryMoveXY(entity, position, stepMove);

        public virtual SectorMoveStatus MoveSectorZ(Sector sector, SectorPlane sectorPlane, SectorPlaneType moveType,
            MoveDirection direction, double speed, double destZ, CrushData? crush)
             => PhysicsManager.MoveSectorZ(sector, sectorPlane, moveType, direction, speed, destZ, crush);

        public virtual void DisplayMessage(Player player, string message)
        {
            Log.Info(message);
        }

        private void ApplyExplosionDamageAndThrust(Entity source, Entity entity, double radius, Thrust thrust)
        {
            double distance;

            if (thrust == Thrust.HorizontalAndVertical && (source.Position.Z < entity.Position.Z || source.Position.Z >= entity.Box.Top))
            {
                Vec3D sourcePos = source.Position;
                Vec3D targetPos = entity.Position;

                if (source.Position.Z > entity.Position.Z)
                    targetPos.Z += entity.Height;

                distance = Math.Max(0.0, sourcePos.Distance(targetPos) - entity.Radius);
            }
            else
            {
                distance = entity.Position.To2D().Distance(source.Position.To2D()) - entity.Radius;
            }

            int damage = (int)(radius - distance);
            if (damage <= 0)
                return;

            DamageEntity(entity, source, damage, thrust);
        }

        protected void ChangeToLevel(int number)
        {
            LevelExit?.Invoke(this, new LevelChangeEvent(number));
        }

        protected void ResetLevel()
        {
            LevelExit?.Invoke(this, new LevelChangeEvent(LevelChangeType.Reset));
        }

        protected virtual void PerformDispose()
        {
            EntityManager.Dispose();
            SoundManager.Dispose();
        }

        private void DebugHitscanTest(in BlockmapIntersect bi, Vec3D intersect)
        {
            string className = bi.Entity == null || bi.Entity.Definition.Flags.NoBlood ? "BulletPuff" : bi.Entity.GetBloodType();
            EntityManager.Create(className, intersect);
        }

        private void MoveIntersectCloser(in Vec3D start, ref Vec3D intersect, double angle, double distXY)
        {
            distXY -= 2.0;
            intersect.X = start.X + (Math.Cos(angle) * distXY);
            intersect.Y = start.Y + (Math.Sin(angle) * distXY);
        }

        private bool GetAutoAimAngle(Entity shooter, in Vec3D start, in Vec3D end, out double pitch, out Entity? entity)
        {
            Seg2D seg = new(start.To2D(), end.To2D());

            List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(seg,
                BlockmapTraverseFlags.Entities | BlockmapTraverseFlags.Lines,
                BlockmapTraverseEntityFlags.Shootable | BlockmapTraverseEntityFlags.Solid);

            return GetBlockmapTraversalPitch(intersections, start, shooter, MaxPitch, MinPitch, out pitch, out entity) == TraversalPitchStatus.PitchSet;
        }

        private enum TraversalPitchStatus
        {
            Blocked,
            PitchSet,
            PitchNotSet,
        }

        private TraversalPitchStatus GetBlockmapTraversalPitch(List<BlockmapIntersect> intersections, in Vec3D start, Entity startEntity, double topPitch, double bottomPitch,
            out double pitch, out Entity? entity)
        {
            pitch = 0.0;
            entity = null;

            for (int i = 0; i < intersections.Count; i++)
            {
                BlockmapIntersect bi = intersections[i];

                if (bi.Line != null)
                {
                    if (bi.Line.Back == null)
                        return TraversalPitchStatus.Blocked;

                    LineOpening opening = PhysicsManager.GetLineOpening(bi.Intersection, bi.Line);
                    if (opening.FloorZ < opening.CeilingZ)
                    {
                        double sectorPitch = start.Pitch(opening.FloorZ, bi.Distance2D);
                        if (sectorPitch > bottomPitch)
                            bottomPitch = sectorPitch;

                        sectorPitch = start.Pitch(opening.CeilingZ, bi.Distance2D);
                        if (sectorPitch < topPitch)
                            topPitch = sectorPitch;

                        if (topPitch <= bottomPitch)
                            return TraversalPitchStatus.Blocked;
                    }
                    else
                    {
                        return TraversalPitchStatus.Blocked;
                    }
                }
                else if (bi.Entity != null && !ReferenceEquals(startEntity, bi.Entity))
                {
                    double thingTopPitch = start.Pitch(bi.Entity.Box.Max.Z, bi.Distance2D);
                    double thingBottomPitch = start.Pitch(bi.Entity.Box.Min.Z, bi.Distance2D);

                    if (thingBottomPitch > topPitch)
                        return TraversalPitchStatus.Blocked;
                    if (thingTopPitch < bottomPitch)
                        return TraversalPitchStatus.Blocked;

                    if (thingTopPitch < topPitch)
                        topPitch = thingTopPitch;
                    if (thingBottomPitch > bottomPitch)
                        bottomPitch = thingBottomPitch;

                    pitch = (bottomPitch + topPitch) / 2.0;
                    entity = bi.Entity;
                    return TraversalPitchStatus.PitchSet;
                }
            }

            return TraversalPitchStatus.PitchNotSet;
        }

        private void HandleEntityDeath(Entity deathEntity)
        {
            PhysicsManager.HandleEntityDeath(deathEntity);
        }

        private bool IsSkyClipOneSided(Sector sector, double floorZ, double ceilingZ, in Vec3D intersect)
        {
            if (intersect.Z > ceilingZ && sector.Ceiling.Texture.IsSky)
                return true;
            if (intersect.Z < floorZ && sector.Floor.Texture.IsSky)
                return true;

            return false;
        }

        private bool IsSkyClipTwoSided(Sector front, Sector back, in Vec3D intersect)
        {
            bool isFrontCeilingSky = front.Ceiling.Texture.IsSky;
            bool isBackCeilingSky = back.Ceiling.Texture.IsSky;

            if (isFrontCeilingSky && isBackCeilingSky && intersect.Z > back.ToCeilingZ(intersect))
                return true;

            if (isFrontCeilingSky && intersect.Z > front.ToCeilingZ(intersect))
                return true;

            if (front.Floor.Texture.IsSky && intersect.Z < front.ToFloorZ(intersect))
                return true;

            return false;
        }

        private static void GetSectorPlaneIntersection(in Vec3D start, in Vec3D end, Sector sector, double floorZ, double ceilingZ, ref Vec3D intersect)
        {
            if (intersect.Z < floorZ)
            {
                sector.Floor.Plane.Intersects(start, end, ref intersect);
                intersect.Z = sector.ToFloorZ(intersect);
            }
            else if (intersect.Z > ceilingZ)
            {
                sector.Ceiling.Plane.Intersects(start, end, ref intersect);
                intersect.Z = sector.ToCeilingZ(intersect) - 4;
            }
        }

        private static void GetOrderedSectors(Line line, in Vec3D start, out Sector front, out Sector back)
        {
            if (line.Segment.OnRight(start))
            {
                front = line.Front.Sector;
                back = line.Back!.Sector;
            }
            else
            {
                front = line.Back!.Sector;
                back = line.Front.Sector;
            }
        }
    }
}