﻿using System;
using System.Collections.Generic;
using Helion.Audio;
using Helion.Maps;
using Helion.Maps.Specials.Compatibility;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Locks;
using Helion.Resources.Definitions.Language;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using Helion.Util.RandomGenerators;
using Helion.World.Blockmap;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Entities.Definition.Properties.Components;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;
using Helion.World.Physics.Blockmap;
using Helion.World.Sound;
using Helion.World.Special;
using Helion.World.Special.SectorMovement;
using MoreLinq;
using NLog;
using static Helion.Util.Assertion.Assert;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Container;
using Helion.World.Entities.Definition;
using Helion.Models;
using Helion.Util.Timing;
using Helion.World.Entities.Definition.Flags;
using System.Linq;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.World.Cheats;

namespace Helion.World
{
    public abstract partial class WorldBase : IWorld
    {
        private const double MaxPitch = 80.0 * Math.PI / 180.0;
        private const double MinPitch = -80.0 * Math.PI / 180.0;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Fires when an entity activates a line special with use or by crossing a line.
        /// </summary>
        public event EventHandler<EntityActivateSpecialEventArgs>? EntityActivatedSpecial;
        public event EventHandler<LevelChangeEvent>? LevelExit;

        public readonly long CreationTimeNanos;
        public CIString MapName { get; protected set; }
        public readonly BlockMap Blockmap;
        public WorldState WorldState { get; protected set; } = WorldState.Normal;
        public int Gametick { get; private set; }
        public int LevelTime { get; private set; }
        public double Gravity { get; private set; } = 1.0;
        public bool Paused { get; private set; }
        public IRandom Random => m_random;
        public IList<Line> Lines => Geometry.Lines;
        public IList<Side> Sides => Geometry.Sides;
        public IList<Wall> Walls => Geometry.Walls;
        public IList<Sector> Sectors => Geometry.Sectors;
        public BspTree BspTree => Geometry.BspTree;
        public LinkableList<Entity> Entities => EntityManager.Entities;
        public EntityManager EntityManager { get; }
        public WorldSoundManager SoundManager { get; }
        public abstract Vec3D ListenerPosition { get; }
        public abstract double ListenerAngle { get; }
        public abstract double ListenerPitch { get; }
        public abstract Entity ListenerEntity { get; }
        public BlockmapTraverser BlockmapTraverser => PhysicsManager.BlockmapTraverser;
        public Config Config { get; private set; }
        public MapInfoDef MapInfo { get; private set; }
        public SkillDef SkillDefinition { get; private set; }
        public ArchiveCollection ArchiveCollection { get; protected set; }

        protected readonly IAudioSystem AudioSystem;
        protected readonly MapGeometry Geometry;
        protected readonly SpecialManager SpecialManager;
        protected readonly PhysicsManager PhysicsManager;
        protected readonly IMap Map;
        private readonly DoomRandom m_random = new();

        private int m_exitTicks;
        private int m_easyBossBrain;
        private int m_soundCount;
        private LevelChangeType m_levelChangeType = LevelChangeType.Next;
        private Entity[] m_bossBrainTargets = Array.Empty<Entity>();

        protected WorldBase(Config config, ArchiveCollection archiveCollection, IAudioSystem audioSystem,
            MapGeometry geometry, MapInfoDef mapInfoDef, SkillDef skillDef, IMap map, WorldModel? worldModel = null)
        {
            CreationTimeNanos = Ticker.NanoTime();
            ArchiveCollection = archiveCollection;
            AudioSystem = audioSystem;
            Config = config;
            MapInfo = mapInfoDef;
            SkillDefinition = skillDef;
            MapName = map.Name;
            Geometry = geometry;
            Map = map;
            Blockmap = new BlockMap(Lines);
            SoundManager = new WorldSoundManager(this, audioSystem, archiveCollection);
            EntityManager = new EntityManager(this, archiveCollection, SoundManager);
            PhysicsManager = new PhysicsManager(this, BspTree, Blockmap, SoundManager, EntityManager, m_random);
            SpecialManager = new SpecialManager(this, archiveCollection.Definitions, m_random);

            if (worldModel == null)
            {
                SpecialManager.StartInitSpecials();
            }
            else
            {
                WorldState = worldModel.WorldState;
                Gametick = worldModel.Gametick;
                LevelTime = worldModel.LevelTime;
                m_soundCount = worldModel.SoundCount;
                Gravity = worldModel.Gravity;
                ((DoomRandom)Random).RandomIndex = worldModel.RandomIndex;
            }
        }

        ~WorldBase()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public virtual void Start()
        {
            AddMapSpecial();
            InitBossBrainTargets();
        }

        public Player? GetLineOfSightPlayer(Entity entity, bool allaround)
        {
            for (int i = 0; i < EntityManager.Players.Count; i++)
            {
                Player player = EntityManager.Players[i];
                if (player.IsDead)
                    continue;

                double distance = entity.Position.ApproximateDistance2D(player.Position);

                if (!allaround)
                {
                    Vec2D entityLookingVector = Vec2D.UnitCircle(entity.AngleRadians);
                    Vec2D entityToTarget = player.Position.XY - entity.Position.XY;

                    // Not in front 180 FOV
                    if (entityToTarget.Dot(entityLookingVector) < 0 && distance > Constants.EntityMeleeDistance)
                        continue;
                }

                if (CheckLineOfSight(entity, player))
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

                Sector other = line.Front.Sector == sector ? line.Back.Sector : line.Front.Sector;
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
            if (Paused)
                return;

            if (WorldState == WorldState.Exit)
            {
                SoundManager.Tick();
                m_exitTicks--;
                if (m_exitTicks == 0)
                {
                    LevelExit?.Invoke(this, new LevelChangeEvent(m_levelChangeType));
                    WorldState = WorldState.Exited;
                }
            }
            else if (WorldState == WorldState.Normal)
            {
                foreach (Entity entity in EntityManager.Entities)
                {
                    entity.Tick();

                    // Entities can be disposed after Tick() (rocket explosion, blood spatter etc.)
                    if (!entity.IsDisposed)
                        PhysicsManager.Move(entity);

                    if (entity.Respawn)
                        HandleRespawn(entity);

                    if (WorldState == WorldState.Exit)
                        return;
                }

                foreach (Player player in EntityManager.Players)
                {
                    // Doom did not apply sector damage to voodoo dolls
                    if (player.IsVooDooDoll)
                        continue;

                    if (player.Sector.SectorDamageSpecial != null)
                        player.Sector.SectorDamageSpecial.Tick(player);

                    if (player.Sector.SectorSpecialType == ZDoomSectorSpecialType.Secret)
                    {
                        DisplayMessage(player, null, "$SECRETMESSAGE", LanguageMessageType.Default);
                        SoundManager.PlayStaticSound("misc/secret");
                        player.Sector.SetSectorSpecialType(ZDoomSectorSpecialType.None);
                        player.SecretsFound++;
                    }
                }

                SpecialManager.Tick();
                TextureManager.Instance.Tick();
                SoundManager.Tick();

                LevelTime++;
            }

            Gametick++;
        }

        public void Pause()
        {
            if (Paused)
                return;

            ResetInterpolation();
            SoundManager.Pause();

            Paused = true;
        }

        private void ResetInterpolation()
        {
            EntityManager.Entities.ForEach(entity =>
            {
                entity.ResetInterpolation();
            });

            SpecialManager.ResetInterpolation();
        }

        public void Resume()
        {
            if (!Paused)
                return;

            SoundManager.Resume();
            Paused = false;
        }

        private void AddMapSpecial()
        {
            List<MonsterCountSpecial> monsterCountSpecials = new List<MonsterCountSpecial>();

            switch (MapInfo.MapSpecial)
            {
                case MapSpecial.BaronSpecial:
                    AddMonsterCountSpecial(monsterCountSpecials, "BaronOfHell", 666, MapInfo.MapSpecialAction);
                    break;
                case MapSpecial.CyberdemonSpecial:
                    AddMonsterCountSpecial(monsterCountSpecials, "Cyberdemon", 666, MapInfo.MapSpecialAction);
                    break;
                case MapSpecial.SpiderMastermindSpecial:
                    AddMonsterCountSpecial(monsterCountSpecials, "SpiderMastermind", 666, MapInfo.MapSpecialAction);
                    break;
                case MapSpecial.Map07Special:
                    AddMonsterCountSpecial(monsterCountSpecials, "Fatso", 666, MapSpecialAction.LowerFloor);
                    AddMonsterCountSpecial(monsterCountSpecials, "Arachnotron", 667, MapSpecialAction.FloorRaiseByLowestTexture);
                    break;
            }

            monsterCountSpecials.ForEach(x => SpecialManager.AddSpecial(x));
        }

        private void AddMonsterCountSpecial(List<MonsterCountSpecial> monsterCountSpecials, string monsterName, int sectorTag, MapSpecialAction mapSpecialAction)
        {
            EntityDefinition? definition = EntityManager.DefinitionComposer.GetByName(monsterName);
            if (definition == null || !definition.EditorId.HasValue)
            {
                Log.Error($"Failed to find {monsterName} for {mapSpecialAction}");
                return;
            }

            monsterCountSpecials.Add(new MonsterCountSpecial(this, SpecialManager, definition.EditorId.Value, sectorTag, mapSpecialAction));
        }

        private void InitBossBrainTargets()
        {
            // Doom chose for some reason to iterate in reverse order.
            m_bossBrainTargets = EntityManager.Entities.Where(e => e.Definition.Name == "BOSSTARGET")
                .Reverse()
                .ToArray();
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

            ResetInterpolation();
        }

        public Entity[] GetBossTargets()
        {
            m_easyBossBrain ^= 1;
            if (SkillDefinition.EasyBossBrain && m_easyBossBrain == 0)
                return Array.Empty<Entity>();

            return m_bossBrainTargets;
        }

        public int CurrentBossTarget { get; set; }

        public void TelefragBlockingEntities(Entity entity)
        {
            List<Entity> blockingEntities = entity.GetIntersectingEntities3D(entity.Position, BlockmapTraverseEntityFlags.Solid | BlockmapTraverseEntityFlags.Shootable);
            for (int i = 0; i < blockingEntities.Count; i++)
                KillEntity(blockingEntities[i], entity, true);
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

            bool hitBlockLine = false;
            bool activateSuccess = false;
            Vec2D start = entity.Position.XY;
            Vec2D end = start + (Vec2D.UnitCircle(entity.AngleRadians) * entity.Properties.Player.UseRange);
            List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(new Seg2D(start, end), BlockmapTraverseFlags.Lines);

            for (int i = 0; i < intersections.Count; i++)
            {
                BlockmapIntersect bi = intersections[i];
                if (bi.Line != null)
                {
                    if (bi.Line.Segment.OnRight(start))
                    {
                        if (bi.Line.HasSpecial)
                            activateSuccess = ActivateSpecialLine(entity, bi.Line, ActivationContext.UseLine) || activateSuccess;

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

            DataCache.Instance.FreeBlockmapIntersectList(intersections);

            if (!activateSuccess && hitBlockLine && entity is Player player)
                player.PlayUseFailSound();

            return activateSuccess;
        }

        public bool CanActivate(Entity entity, Line line, ActivationContext context)
        {
            bool success = line.Special.CanActivate(entity, line, context,
                ArchiveCollection.Definitions.LockDefininitions, out LockDef? lockFail);
            if (entity is Player player && lockFail != null)
            {
                player.PlayUseFailSound();
                DisplayMessage(player, null, GetLockFailMessage(line, lockFail), LanguageMessageType.Lock);
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
            return args.Success;
        }

        public bool GetAutoAimEntity(Entity startEntity, in Vec3D start, double angle, double distance, out double pitch, out Entity? entity)
        {
            Vec3D end = start + Vec3D.UnitSphere(angle, 0) * distance;
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
                Vec3D end = start + Vec3D.UnitSphere(shooter.AngleRadians, pitch) * distance;
                if (GetAutoAimAngle(shooter, start, end, out double autoAimPitch, out _))
                    pitch = autoAimPitch;
            }

            var projectileDef = EntityManager.DefinitionComposer.GetByName(projectClassName);
            if (projectileDef != null)
            {
                Entity projectile = EntityManager.Create(projectileDef, start, 0.0, shooter.AngleRadians, 0);
                Vec3D velocity = Vec3D.UnitSphere(shooter.AngleRadians, pitch) * projectile.Properties.Speed;
                Vec3D testPos = projectile.Position + (Vec3D.UnitSphere(shooter.AngleRadians, pitch) * (shooter.Radius - 2.0));
                projectile.Owner = shooter;
                projectile.PlaySeeSound();

                // TryMoveXY will use the velocity of the projectile
                // A projectile spawned where it can't fit can cause BlockingSectorPlane or BlockingEntity (IsBlocked = true)
                if (projectile.Flags.NoClip || (!projectile.IsBlocked() && PhysicsManager.TryMoveXY(projectile, testPos.XY, true).Success))
                {
                    projectile.Velocity = velocity;
                    return projectile;
                }
                else
                {
                    projectile.SetPosition(testPos);
                    HandleEntityHit(projectile, velocity, null);
                }
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
                Vec3D end = start + Vec3D.UnitSphere(shooter.AngleRadians, pitch) * distance;
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
            Vec3D end = start + Vec3D.UnitSphere(angle, pitch) * distance;
            Vec3D intersect = new Vec3D(0, 0, 0);

            BlockmapIntersect? bi = FireHitScan(shooter, start, end, pitch, ref intersect, out Sector? hitSector);

            if (bi != null)
            {
                if (damage > 0)
                {
                    // Only move closer on a line hit
                    if (bi.Value.Entity == null && hitSector == null)
                        MoveIntersectCloser(start, ref intersect, angle, bi.Value.Distance2D);
                    HitscanHit(bi.Value, intersect, distance, damage);
                }

                if (bi.Value.Entity != null)
                {
                    DamageEntity(bi.Value.Entity, shooter, damage, Thrust.Horizontal);
                    return bi.Value.Entity;
                }
            }

            return null;
        }

        public virtual BlockmapIntersect? FireHitScan(Entity shooter, Vec3D start, Vec3D end, double pitch, ref Vec3D intersect,
            out Sector? hitSector)
        {
            hitSector = null;
            BlockmapIntersect? returnValue = null;
            double floorZ, ceilingZ;
            Seg2D seg = new(start.XY, end.XY);
            List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(seg,
                BlockmapTraverseFlags.Entities | BlockmapTraverseFlags.Lines,
                BlockmapTraverseEntityFlags.Shootable | BlockmapTraverseEntityFlags.Solid);

            for (int i = 0; i < intersections.Count; i++)
            {
                BlockmapIntersect bi = intersections[i];

                if (bi.Line != null)
                {
                    if (bi.Line.HasSpecial && CanActivate(shooter, bi.Line, ActivationContext.ProjectileHitLine))
                    {
                        var args = new EntityActivateSpecialEventArgs(ActivationContext.ProjectileHitLine, shooter, bi.Line);
                        EntityActivatedSpecial?.Invoke(this, args);
                    }

                    intersect = bi.Intersection.To3D(start.Z + (Math.Tan(pitch) * bi.Distance2D));

                    if (bi.Line.Back == null)
                    {
                        floorZ = bi.Line.Front.Sector.ToFloorZ(intersect);
                        ceilingZ = bi.Line.Front.Sector.ToCeilingZ(intersect);

                        if (intersect.Z > floorZ && intersect.Z < ceilingZ)
                        {
                            returnValue = bi;
                            break;
                        }

                        if (IsSkyClipOneSided(bi.Line.Front.Sector, floorZ, ceilingZ, intersect))
                            break;

                        GetSectorPlaneIntersection(start, end, bi.Line.Front.Sector, floorZ, ceilingZ, ref intersect);
                        hitSector = bi.Line.Front.Sector;
                        return bi;
                    }

                    GetOrderedSectors(bi.Line, start, out Sector front, out Sector back);
                    if (IsSkyClipTwoSided(front, back, intersect))
                        break;

                    floorZ = front.ToFloorZ(intersect);
                    ceilingZ = front.ToCeilingZ(intersect);

                    if (intersect.Z < floorZ || intersect.Z > ceilingZ)
                    {
                        GetSectorPlaneIntersection(start, end, front, floorZ, ceilingZ, ref intersect);
                        hitSector = front;
                        returnValue = bi;
                        break;
                    }

                    LineOpening opening = PhysicsManager.GetLineOpening(bi.Intersection, bi.Line);
                    if ((opening.FloorZ > intersect.Z && intersect.Z > floorZ) || (opening.CeilingZ < intersect.Z && intersect.Z < ceilingZ))
                    {
                        returnValue = bi;
                        break;
                    }
                }
                else if (bi.Entity != null && !ReferenceEquals(shooter, bi.Entity) && bi.Entity.Box.Intersects(start, end, ref intersect))
                {
                    returnValue = bi;
                    break;
                }
            }

            DataCache.Instance.FreeBlockmapIntersectList(intersections);
            return returnValue;
        }

        public virtual bool DamageEntity(Entity target, Entity? source, int damage, 
            Thrust thrust = Thrust.HorizontalAndVertical, Sector? sectorSource = null)
        {
            if (!target.Flags.Shootable || damage == 0)
                return false;

            Vec3D thrustVelocity = Vec3D.Zero;

            if (source != null && thrust != Thrust.None)
            {
                Vec2D xyDiff = source.Position.XY - target.Position.XY;
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
                        pitch = sourcePos.Pitch(targetPos, sourcePos.XY.Distance(targetPos.XY));
                    }

                    if (!xyEqual)
                        thrustVelocity = Vec3D.UnitSphere(angle, 0.0);

                    thrustVelocity.Z = Math.Sin(pitch);
                }
                else
                {
                    thrustVelocity = Vec3D.UnitSphere(angle, 0.0);
                }

                thrustVelocity *= thrustAmount;
            }

            bool setPainState = m_random.NextByte() < target.Properties.PainChance;
            if (target is Player player)
            {
                // Voodoo dolls did not take sector damage in the original
                if (player.IsVooDooDoll && sectorSource != null)
                    return false;
                // Sector damage is applied to real players, but not their voodoo dolls
                if (sectorSource == null)
                    ApplyVooDooDamage(player, damage, setPainState);
            }

            if (target.Damage(source, damage, setPainState) || target.IsInvulnerable)
                target.Velocity += thrustVelocity;

            return true;
        }

        public void KillEntity(Entity entity, Entity? source, bool forceGib = false)
        {
            if (entity.IsDead)
                return;

            if (entity is Player player)
                ApplyVooDooKill(player, source, forceGib);

            if (forceGib)
                entity.ForceGib();
            else
                entity.Kill(source);
        }

        public virtual bool GiveItem(Player player, Entity item, EntityFlags? flags, bool pickupFlash = true)
        {
            GiveVooDooItem(player, item, flags, pickupFlash);
            return player.GiveItem(item.Definition, flags, pickupFlash);
        }

        public virtual void PerformItemPickup(Entity entity, Entity item)
        {
            if (entity is not Player player)
                return;

            int health = player.Health;
            if (!GiveItem(player, item, item.Flags))
                return;

            string message = item.Definition.Properties.Inventory.PickupMessage;
            var healthProperty = item.Definition.Properties.HealthProperty;
            if (healthProperty != null && health < healthProperty.Value.LowMessageHealth && healthProperty.Value.LowMessage.Length > 0)
                message = healthProperty.Value.LowMessage;

            DisplayMessage(player, null, message, LanguageMessageType.Pickup);
            EntityManager.Destroy(item);

            if (!string.IsNullOrEmpty(item.Definition.Properties.Inventory.PickupSound))
            {
                SoundManager.CreateSoundOn(entity, item.Definition.Properties.Inventory.PickupSound, SoundChannelType.Item,
                    new SoundParams(entity));
            }
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

                if (entity.BlockingSectorPlane != null && TextureManager.Instance.IsSkyTexture(entity.BlockingSectorPlane.TextureHandle))
                    skyClip = true;

                if (skyClip)
                    EntityManager.Destroy(entity);
                else
                    entity.SetDeathState(null);
            }
            else if (tryMove != null && entity is Player)
            {
                for (int i = 0; i < tryMove.IntersectSpecialLines.Count; i++)
                    ActivateSpecialLine(entity, tryMove.IntersectSpecialLines[i], ActivationContext.PlayerPushesWall);
            }
        }

        public virtual bool CheckLineOfSight(Entity from, Entity to)
        {
            Vec2D start = from.Position.XY;
            Vec2D end = to.Position.XY;

            if (start == end)
                return true;

            Seg2D seg = new Seg2D(start, end);

            List<BlockmapIntersect> intersections = BlockmapTraverser.Traverse(null, seg, BlockmapTraverseFlags.Lines | BlockmapTraverseFlags.StopOnOneSidedLine,
                BlockmapTraverseEntityFlags.None, out bool hitOneSidedLine);
            if (hitOneSidedLine)
            {
                DataCache.Instance.FreeBlockmapIntersectList(intersections);
                return false;
            }

            Vec3D sightPos = new Vec3D(from.Position.X, from.Position.Y, from.Position.Z + (from.Height * 0.75));
            double distance2D = start.Distance(end);
            double topPitch = sightPos.Pitch(to.Position.Z + to.Height, distance2D);
            double bottomPitch = sightPos.Pitch(to.Position.Z, distance2D);

            TraversalPitchStatus status = GetBlockmapTraversalPitch(intersections, sightPos, from, topPitch, bottomPitch, out _, out _);
            DataCache.Instance.FreeBlockmapIntersectList(intersections);
            return  status != TraversalPitchStatus.Blocked;
        }

        public virtual void RadiusExplosion(Entity source, int radius)
        {
            // Barrels do not apply Z thrust - TODO better way to check?
            Thrust thrust = source.Definition.Name == "ExplosiveBarrel" ? Thrust.Horizontal : Thrust.HorizontalAndVertical;
            Vec2D pos2D = source.Position.XY;
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

            DataCache.Instance.FreeBlockmapIntersectList(intersections);
        }

        public virtual TryMoveData TryMoveXY(Entity entity, Vec2D position, bool stepMove = true)
            => PhysicsManager.TryMoveXY(entity, position, stepMove);

        public virtual SectorMoveStatus MoveSectorZ(Sector sector, SectorPlane sectorPlane, SectorPlaneType moveType,
            double speed, double destZ, CrushData? crush)
             => PhysicsManager.MoveSectorZ(sector, sectorPlane, moveType, speed, destZ, crush);

        public virtual void HandleEntityDeath(Entity deathEntity, Entity? deathSource)
        {
            PhysicsManager.HandleEntityDeath(deathEntity);
            CheckDropItem(deathEntity);

            if (deathSource != null && deathEntity is Player player)
                HandleObituary(player, deathSource);
        }

        private void CheckDropItem(Entity deathEntity)
        {
            if (deathEntity.Definition.Properties.DropItem != null &&
                (deathEntity.Definition.Properties.DropItem.Probability == DropItemProperty.DefaultProbability ||
                    m_random.NextByte() < deathEntity.Definition.Properties.DropItem.Probability))
            {
                for (int i = 0; i < deathEntity.Definition.Properties.DropItem.Amount; i++)
                {
                    Vec3D pos = deathEntity.Position;
                    pos.Z += deathEntity.Definition.Properties.Height / 2;
                    Entity? dropItem = EntityManager.Create(deathEntity.Definition.Properties.DropItem.ClassName, pos);
                    if (dropItem != null)
                    {
                        dropItem.Flags.Dropped = true;
                        dropItem.Velocity.Z += 4;
                    }
                }
            }
        }

        private void HandleObituary(Player player, Entity deathSource)
        {
            // If the player killed themself then don't display the obituary message
            // There is probably a special string for this in multiplayer for later
            Entity killer = deathSource.Owner ?? deathSource;
            if (ReferenceEquals(player, killer))
                return;

            // Monster obituaries can come from the projectile, while the player obituaries always come from the owner player
            Entity obituarySource = killer;
            if (killer is Player)
                obituarySource = deathSource;

            string? obituary;
            if (obituarySource == deathSource && obituarySource.Definition.Properties.HitObituary.Length > 0)
                obituary = obituarySource.Definition.Properties.HitObituary;
            else
                obituary = obituarySource.Definition.Properties.Obituary;

            if (!string.IsNullOrEmpty(obituary))
                DisplayMessage(player, killer as Player, obituary, LanguageMessageType.Obituary);
        }

        public virtual void DisplayMessage(Player player, Player? other, string message, LanguageMessageType type)
        {
            message = ArchiveCollection.Definitions.Language.GetMessage(player, other, message, type);
            if (message.Length > 0)
                Log.Error(message);
        }

        private void HandleRespawn(Entity entity)
        {
            entity.Respawn = false;
            if (entity.Definition.Flags.Solid && IsPositionBlockedByEntity(entity, entity.SpawnPoint))
                return;

            Entity? newEntity = EntityManager.Create(entity.Definition, entity.SpawnPoint, 0, entity.AngleRadians, entity.ThingId, true);
            if (newEntity != null)
            {
                CreateTeleportFog(entity.Position);
                CreateTeleportFog(entity.SpawnPoint);

                newEntity.SetSpawnState();
                newEntity.AngleRadians = entity.AngleRadians;
                newEntity.ReactionTime = 18;

                entity.Dispose();
            }
        }

        public bool IsPositionBlockedByEntity(Entity entity, in Vec3D position)
        {
            if (!entity.Definition.Flags.Solid)
                return true;

            double oldHeight = entity.Height;
            entity.Flags.Solid = true;
            entity.SetHeight(entity.Definition.Properties.Height);

            // This is original functionality, the original game only checked against other things
            // It didn't check if it would clip into map geometry
            bool blocked = entity.GetIntersectingEntities3D(position, BlockmapTraverseEntityFlags.Solid).Count > 0;
            entity.Flags.Solid = false;
            entity.SetHeight(oldHeight);

            return blocked;
        }

        public bool IsPositionBlocked(Entity entity)
        {
            if (entity.GetIntersectingEntities3D(entity.Position, BlockmapTraverseEntityFlags.Solid).Count > 0)
                return true;

            if (!PhysicsManager.IsPositionValid(entity, entity.Position.XY, new TryMoveData()))
                return true;

            return false;
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
                distance = entity.Position.XY.Distance(source.Position.XY) - entity.Radius;
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
            SpecialManager.Dispose();
            EntityManager.Dispose();
            SoundManager.Dispose();
        }

        private void HitscanHit(in BlockmapIntersect bi, Vec3D intersect, double distance, int damage)
        {
            bool bulletPuff = bi.Entity == null || bi.Entity.Definition.Flags.NoBlood;
            string className;
            if (bulletPuff)
            {
                className = "BulletPuff";
                intersect.Z += Random.NextDiff() * Constants.PuffRandZ;
            }
            else
            {
                className = bi.Entity!.GetBloodType();
            }
            
            Entity? entity = EntityManager.Create(className, intersect);
            if (entity == null)
                return;

            if (bulletPuff)
            {
                entity.Velocity.Z = 1;
                entity.FrameState.SetTics(entity.FrameState.CurrentTick - (Random.NextByte() & 3));

                // Doom would skip the initial sparking state of the bullet puff for punches
                // Bulletpuff decorate has a MELEESTATE for this
                if (distance == Constants.EntityMeleeDistance)
                    entity.SetMeleeState();
            }
            else
            {
                entity.Velocity.Z = 2;
                entity.FrameState.SetTics(Random.NextByte() & 3);

                int offset = 0;
                if (damage <= 12 && damage >= 9)
                    offset = 1;
                else if (damage < 9)
                    offset = 2;

                entity.FrameState.SetState("SPAWN", offset);
            }
        }

        private void MoveIntersectCloser(in Vec3D start, ref Vec3D intersect, double angle, double distXY)
        {
            distXY -= 2.0;
            intersect.X = start.X + (Math.Cos(angle) * distXY);
            intersect.Y = start.Y + (Math.Sin(angle) * distXY);
        }

        private bool GetAutoAimAngle(Entity shooter, in Vec3D start, in Vec3D end, out double pitch, out Entity? entity)
        {
            Seg2D seg = new Seg2D(start.XY, end.XY);

            List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(seg,
                BlockmapTraverseFlags.Entities | BlockmapTraverseFlags.Lines,
                BlockmapTraverseEntityFlags.Shootable | BlockmapTraverseEntityFlags.Solid);

            TraversalPitchStatus status = GetBlockmapTraversalPitch(intersections, start, shooter, MaxPitch, MinPitch, out pitch, out entity);
            DataCache.Instance.FreeBlockmapIntersectList(intersections);
            return status == TraversalPitchStatus.PitchSet;
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
                    if (thingTopPitch < bottomPitch)
                        continue;

                    double thingBottomPitch = start.Pitch(bi.Entity.Box.Min.Z, bi.Distance2D);
                    if (thingBottomPitch > topPitch)
                        continue;

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

        private bool IsSkyClipOneSided(Sector sector, double floorZ, double ceilingZ, in Vec3D intersect)
        {
            if (intersect.Z > ceilingZ && TextureManager.Instance.IsSkyTexture(sector.Ceiling.TextureHandle))
                return true;
            else if (intersect.Z < floorZ && TextureManager.Instance.IsSkyTexture(sector.Floor.TextureHandle))
                return true;

            return false;
        }

        private bool IsSkyClipTwoSided(Sector front, Sector back, in Vec3D intersect)
        {
            bool isFrontCeilingSky = TextureManager.Instance.IsSkyTexture(front.Ceiling.TextureHandle);
            bool isBackCeilingSky = TextureManager.Instance.IsSkyTexture(back.Ceiling.TextureHandle);

            if (isFrontCeilingSky && isBackCeilingSky && intersect.Z > back.ToCeilingZ(intersect))
                return true;

            if (isFrontCeilingSky && intersect.Z > front.ToCeilingZ(intersect))
                return true;

            if (TextureManager.Instance.IsSkyTexture(front.Floor.TextureHandle) && intersect.Z < front.ToFloorZ(intersect))
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

        public void CreateTeleportFog(in Vec3D pos, bool playSound = true)
        {
            EntityDefinition? teleportFog = EntityManager.DefinitionComposer.GetByName("TeleportFog");
            if (teleportFog != null)
            {
                var teleport = EntityManager.Create(teleportFog, pos, 0.0, 0.0, 0);
                if (teleport != null)
                {
                    teleport.SetZ(teleport.Sector.ToFloorZ(pos), false);
                    SoundManager.CreateSoundOn(teleport, Constants.TeleportSound, SoundChannelType.Auto, new SoundParams(teleport));
                }
            }
        }

        public void ActivateCheat(Player player, ICheat cheat)
        {
            if (cheat is ChangeLevelCheat changeLevel)
            {
                ChangeToLevel(changeLevel.LevelNumber);
                return;
            }

            switch (cheat.CheatType)
            {
                case CheatType.NoClip:
                    player.Flags.NoClip = player.Cheats.IsCheatActive(cheat.CheatType);
                    break;
                case CheatType.Fly:
                    player.Flags.NoGravity = player.Cheats.IsCheatActive(cheat.CheatType);
                    break;
                case CheatType.Ressurect:
                    if (player.IsDead)
                        player.SetRaiseState();
                    break;
                case CheatType.God:
                    player.Health = player.Definition.Properties.Player.MaxHealth;
                    player.Flags.Invulnerable = player.Cheats.IsCheatActive(cheat.CheatType);
                    break;
                case CheatType.GiveAllNoKeys:
                    GiveAllWeapons(player);
                    player.GiveBestArmor(EntityManager.DefinitionComposer);
                    break;
                case CheatType.GiveAll:
                    GiveAllWeapons(player);
                    player.Inventory.GiveAllKeys(EntityManager.DefinitionComposer);
                    player.GiveBestArmor(EntityManager.DefinitionComposer);
                    break;
            }

            string msg;
            if (cheat.IsToggleCheat)
                msg = string.Format("{0} cheat: {1}", player.Cheats.IsCheatActive(cheat.CheatType) ? "Activated" : "Deactivated", cheat.CheatName);
            else
                msg = cheat.CheatName;

            DisplayMessage(player, null, msg, LanguageMessageType.None);
        }

        private void GiveAllWeapons(Player player)
        {
            foreach (CIString name in player.Inventory.Weapons.GetWeaponDefinitionNames())
            {
                var weapon = EntityManager.DefinitionComposer.GetByName(name);
                if (weapon != null)
                    player.GiveWeapon(weapon, autoSwitch: false);
            }

            player.Inventory.GiveAllAmmo(EntityManager.DefinitionComposer);
        }

        private void ApplyVooDooDamage(Player player, int damage, bool setPainState)
        {
            if (EntityManager.VoodooDolls.Count == 0)
                return;

            SyncVooDollsWithPlayer(player.PlayerNumber);

            foreach (var updatePlayer in EntityManager.Players.Union(EntityManager.VoodooDolls))
            {
                if (updatePlayer == player || updatePlayer.PlayerNumber != player.PlayerNumber)
                    continue;

                updatePlayer.Damage(null, damage, setPainState);
            }
        }

        private void ApplyVooDooKill(Player player, Entity? source, bool forceGib)
        {
            if (EntityManager.VoodooDolls.Count == 0)
                return;

            SyncVooDollsWithPlayer(player.PlayerNumber);

            foreach (var updatePlayer in EntityManager.Players.Union(EntityManager.VoodooDolls))
            {
                if (updatePlayer == player || updatePlayer.PlayerNumber != player.PlayerNumber)
                    continue;

                if (forceGib)
                    updatePlayer.ForceGib();
                else
                    updatePlayer.Kill(source);
            }
        }

        private void GiveVooDooItem(Player player, Entity item, EntityFlags? flags, bool pickupFlash)
        {
            if (EntityManager.VoodooDolls.Count == 0)
                return;

            SyncVooDollsWithPlayer(player.PlayerNumber);

            foreach (var updatePlayer in EntityManager.Players.Union(EntityManager.VoodooDolls))
            {
                if (updatePlayer == player || updatePlayer.PlayerNumber != player.PlayerNumber)
                    continue;

                updatePlayer.GiveItem(item.Definition, flags, pickupFlash);

                if (!string.IsNullOrEmpty(item.Definition.Properties.Inventory.PickupSound))
                {
                    SoundManager.CreateSoundOn(updatePlayer, item.Definition.Properties.Inventory.PickupSound, SoundChannelType.Item,
                        new SoundParams(updatePlayer));
                }
            }
        }

        private void SyncVooDollsWithPlayer(int playerNumber)
        {
            Player? realPlayer = GetRealPlayer(playerNumber);
            if (realPlayer == null)
                return;

            foreach (var voodooDoll in EntityManager.VoodooDolls)
                voodooDoll.VodooSync(realPlayer);
        }

        private Player? GetRealPlayer(int playerNumber)
            => EntityManager.Players.FirstOrDefault(x => x.PlayerNumber == playerNumber && !x.IsVooDooDoll);

        public WorldModel ToWorldModel()
        {
            List<SectorModel> sectorModels = new List<SectorModel>();
            List<SectorDamageSpecialModel> sectorDamageSpecialModels = new List<SectorDamageSpecialModel>();
            SetSectorModels(sectorModels, sectorDamageSpecialModels);

            return new WorldModel()
            {
                Files = GetGameFilesModel(),
                MapName = MapName.ToString(),
                WorldState = WorldState,
                Gametick = Gametick,
                LevelTime = LevelTime,
                SoundCount = m_soundCount,
                Gravity = Gravity,
                RandomIndex = ((DoomRandom)Random).RandomIndex,
                Skill = ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkillLevel(SkillDefinition),

                Players = GetPlayerModels(),
                Entities = GetEntityModels(),
                Sectors = sectorModels,
                DamageSpecials = sectorDamageSpecialModels,
                Lines = GetLineModels(),
                Specials = SpecialManager.GetSpecialModels()
            };
        }

        public GameFilesModel GetGameFilesModel()
        {
            return new GameFilesModel()
            {
                IWad = GetIWadFileModel(),
                Files = GetFileModels(),
            };
        }

        private IList<PlayerModel> GetPlayerModels()
        {
            List<PlayerModel> playerModels = new List<PlayerModel>(EntityManager.Players.Count);
            EntityManager.Players.ForEach(player => playerModels.Add(player.ToPlayerModel()));
            EntityManager.VoodooDolls.ForEach(player => playerModels.Add(player.ToPlayerModel()));
            return playerModels;
        }

        private FileModel GetIWadFileModel()
        {
            var archive = ArchiveCollection.GetIWad();
            if (archive != null)
                return archive.ToFileModel();

            return new FileModel();
        }

        private IList<FileModel> GetFileModels()
        {
            List<FileModel> fileModels = new List<FileModel>();
            var archives = ArchiveCollection.GetFiles();
            foreach (var archive in archives)
                fileModels.Add(archive.ToFileModel());

            return fileModels;
        }

        private List<EntityModel> GetEntityModels()
        {
            List<EntityModel> entityModels = new List<EntityModel>();
            EntityManager.Entities.ForEach(entity =>
            {
                if (entity is not Player)
                    entityModels.Add(entity.ToEntityModel(new EntityModel()));
            });
            return entityModels;
        }

        private void SetSectorModels(List<SectorModel> sectorModels, List<SectorDamageSpecialModel> sectorDamageSpecialModels)
        {
            for (int i = 0; i < Sectors.Count; i++)
            {
                Sector sector = Sectors[i];
                if (sector.SoundTarget != null || sector.DataChanged)
                    sectorModels.Add(sector.ToSectorModel());
                if (sector.SectorDamageSpecial != null)
                    sectorDamageSpecialModels.Add(sector.SectorDamageSpecial.ToSectorDamageSpecialModel());
            }
        }

        private List<LineModel> GetLineModels()
        {
            List<LineModel> lineModels = new List<LineModel>();
            for (int i = 0; i < Lines.Count; i++)
            {
                Line line = Lines[i];
                if (!line.DataChanged)
                    continue;

                lineModels.Add(line.ToLineModel());
            }

            return lineModels;
        }
    }
}