using System;
using System.Collections.Generic;
using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Container.Linkable;
using Helion.Util.Extensions;
using Helion.Util.Time;
using Helion.World.Blockmaps;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Geometry;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;
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
        public readonly Blockmap Blockmap;
        public int Gametick { get; private set; }
        protected readonly ArchiveCollection ArchiveCollection;
        protected readonly Config Config;
        protected readonly MapGeometry Geometry;
        protected readonly EntityManager EntityManager;
        protected readonly PhysicsManager PhysicsManager;
        protected readonly SpecialManager SpecialManager;

        public IList<Line> Lines => Geometry.Lines;
        public IList<Side> Sides => Geometry.Sides;
        public IList<Wall> Walls => Geometry.Walls;
        public IList<Sector> Sectors => Geometry.Sectors;
        public BspTree BspTree => Geometry.BspTree;
        public LinkableList<Entity> Entities => EntityManager.Entities;
        
        protected WorldBase(Config config, ArchiveCollection archiveCollection, MapGeometry geometry, IMap map)
        {
            CreationTimeNanos = Ticker.NanoTime();
            ArchiveCollection = archiveCollection;
            Config = config;
            MapName = map.Name;
            Geometry = geometry;
            Blockmap = new Blockmap(Lines);
            PhysicsManager = new PhysicsManager(BspTree, Blockmap); 
            EntityManager = new EntityManager(this, archiveCollection);
            SpecialManager = new SpecialManager(PhysicsManager, this);

            SpecialManager.LevelExit += SpecialManager_LevelExit;
        }

        ~WorldBase()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            PerformDispose();
        }

        public void Link(Entity entity)
        {
            Precondition(entity.SectorNodes.Empty() && entity.BlockmapNodes.Empty(), "Forgot to unlink entity before linking");
            
            PhysicsManager.LinkToWorld(entity);
        }

        public void Tick()
        {
            // TODO: Use me?
            throw new NotImplementedException();
        }

        public void Tick(long gametic)
        {
            // We need to do this (for now) because MoveZ and PreviouslyClipped
            // run into issues if this is not updated properly. If we can do a
            // resolution to the sector moving up/down with clipping monsters
            // issue, then this might be able to be handled better later on.
            EntityManager.Entities.ForEach(entity => entity.PrevPosition = entity.Box.Position);

            EntityManager.Entities.ForEach(entity =>
            {
                entity.Tick();
                PhysicsManager.Move(entity);
            });

            SpecialManager.Tick(gametic);

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

        protected void ChangeToLevel(int number)
        {
            LevelExit?.Invoke(this, new LevelChangeEvent(number));
        }

        protected virtual void PerformDispose()
        {
            SpecialManager.LevelExit -= SpecialManager_LevelExit;
        }

        private void SpecialManager_LevelExit(object? sender, LevelChangeEvent e)
        {
            LevelExit?.Invoke(this, e);
        }
    }
}