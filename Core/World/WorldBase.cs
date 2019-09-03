using System;
using System.Collections.Generic;
using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
using Helion.Util.Container.Linkable;
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
    public abstract class WorldBase : IDisposable
    {
        public event EventHandler<LevelChangeEvent>? LevelExit;

        public readonly long CreationTimeNanos;
        public readonly Blockmap Blockmap;
        public readonly IMap Map;
        public int Gametick { get; private set; }
        protected readonly ArchiveCollection ArchiveCollection;
        protected readonly Config Config;
        protected readonly EntityManager EntityManager;
        protected readonly PhysicsManager PhysicsManager;
        protected readonly SpecialManager SpecialManager;
        private readonly MapGeometry m_geometry;

        public List<Line> Lines => m_geometry.Lines;
        public List<Side> Sides => m_geometry.Sides;
        public List<Wall> Walls => m_geometry.Walls;
        public List<Sector> Sectors => m_geometry.Sectors;
        public BspTree BspTree => m_geometry.BspTree;
        public LinkableList<Entity> Entities => EntityManager.Entities;
        
        protected WorldBase(Config config, ArchiveCollection archiveCollection, MapGeometry geometry, IMap map)
        {
            CreationTimeNanos = Ticker.NanoTime();
            ArchiveCollection = archiveCollection;
            Config = config;
            Map = map;
            m_geometry = geometry;

            Blockmap = new Blockmap(Lines);
            PhysicsManager = new PhysicsManager(BspTree, Blockmap); 
            EntityManager = new EntityManager(this, archiveCollection, PhysicsManager, map);
            SpecialManager = new SpecialManager();

            SpecialManager.LevelExit += SpecialManager_LevelExit;
        }

        ~WorldBase()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            PerformDispose();
        }

        public void Tick(long gametic)
        {
            EntityManager.Players.Values.ForEach(player => player.Tick());
            
            EntityManager.Entities.ForEach(entity =>
            {
                entity.Tick();
                PhysicsManager.Move(entity);
            });

            SpecialManager.Tick(gametic);

            Gametick++;
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