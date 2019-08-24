using System;
using Helion.Maps;
using Helion.Maps.Special;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
using Helion.World.Blockmaps;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Physics;
using MoreLinq;

namespace Helion.World
{
    public abstract class WorldBase
    {
        public event EventHandler LevelExit;

        public int Gametick { get; private set; }
        public readonly IMap Map;
        public readonly BspTree BspTree;
        public readonly Blockmap Blockmap;
        protected readonly ArchiveCollection ArchiveCollection;
        protected readonly Config Config;
        protected readonly EntityManager EntityManager;
        protected readonly PhysicsManager PhysicsManager;
        protected readonly SpecialManager SpecialManager;

        protected WorldBase(Config config, ArchiveCollection archiveCollection, IMap map, BspTree bspTree)
        {
            ArchiveCollection = archiveCollection;
            Config = config;
            Map = map;
            BspTree = bspTree;
            Blockmap = new Blockmap(map);
            PhysicsManager = new PhysicsManager(bspTree, Blockmap); 
            EntityManager = new EntityManager(this, archiveCollection, bspTree, Blockmap, PhysicsManager, map);
            SpecialManager = new SpecialManager(PhysicsManager, Map);

            SpecialManager.LevelExit += SpecialManager_LevelExit;
        }

        private void SpecialManager_LevelExit(object sender, EventArgs e)
        {
            LevelExit?.Invoke(this, e);
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
    }
}