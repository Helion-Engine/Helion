using System;
using System.Collections.Generic;
using Helion.Audio;
using Helion.Maps;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Decorate.Properties;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Container.Linkable;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Vectors;
using Helion.Util.RandomGenerators;
using Helion.Util.Time;
using Helion.World.Blockmap;
using Helion.World.Bsp;
using Helion.World.Entities;
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
using MoreLinq;
using static Helion.Util.Assertion.Assert;

namespace Helion.World
{
    public abstract class WorldBase : IWorld
    {
        public event EventHandler<LevelChangeEvent>? LevelExit;

        public readonly long CreationTimeNanos;
        public readonly CIString MapName;
        public readonly BlockMap Blockmap;
        public PhysicsManager PhysicsManager { get; }
        public WorldState WorldState { get; protected set; } = WorldState.Normal;
        public int Gametick { get; private set; }
        public IRandom Random => m_random;
        protected readonly ArchiveCollection ArchiveCollection;
        protected readonly Config Config;
        protected readonly MapGeometry Geometry;
        protected readonly SpecialManager SpecialManager;

        private int m_exitTicks = 0;
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

        private readonly DoomRandom m_random = new DoomRandom();
        private int m_soundCount;
        
        protected WorldBase(Config config, ArchiveCollection archiveCollection, IAudioSystem audioSystem, 
            MapGeometry geometry, IMap map)
        {
            CreationTimeNanos = Ticker.NanoTime();
            ArchiveCollection = archiveCollection;
            Config = config;
            MapName = map.Name;
            Geometry = geometry;
            Blockmap = new BlockMap(Lines);
            SoundManager = new SoundManager(this, audioSystem, archiveCollection.Definitions.SoundInfo);            
            EntityManager = new EntityManager(this, archiveCollection, SoundManager, config.Engine.Game.Skill);
            PhysicsManager = new PhysicsManager(this, BspTree, Blockmap, SoundManager, EntityManager, m_random);
            SpecialManager = new SpecialManager(this, archiveCollection.Definitions, m_random);
        }

        ~WorldBase()
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

                if (!player.IsDead && PhysicsManager.CheckLineOfSight(entity, player))
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

            foreach (Line line in sector.Lines)
            {
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
            
            PhysicsManager.LinkToWorld(entity);
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
                {
                    if (player.Sector.SectorDamageSpecial != null)
                        player.Sector.SectorDamageSpecial.Tick(player);
                }

                SpecialManager.Tick();
                TextureManager.Instance.Tick();
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
            List<Entity> targets = new List<Entity>();
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
            List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(entity.Box.To2D(), BlockmapTraverseFlags.Entities,
                BlockmapTraverseEntityFlags.Shootable | BlockmapTraverseEntityFlags.Solid);

            for (int i = 0; i < intersections.Count; i++)
            {
                BlockmapIntersect bi = intersections[i];
                if (ReferenceEquals(entity, bi.Entity))
                    continue;
                bi.Entity!.ForceGib();
            }
        }

        protected void ChangeToLevel(int number)
        {
            LevelExit?.Invoke(this, new LevelChangeEvent(number));
        }

        protected virtual void PerformDispose()
        {
            EntityManager.Dispose();
            SoundManager.Dispose();
        }
    }
}