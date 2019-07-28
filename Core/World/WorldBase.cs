using Helion.Cheats;
using Helion.Maps;
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
        public int Gametick { get; private set; }
        public readonly Map Map;
        public readonly BspTree BspTree;
        protected readonly Blockmap Blockmap;
        protected readonly ArchiveCollection ArchiveCollection;
        protected readonly Config Config;
        protected readonly EntityManager EntityManager;
        protected readonly PhysicsManager PhysicsManager;

        protected WorldBase(Config config, ArchiveCollection archiveCollection, Map map, BspTree bspTree)
        {
            ArchiveCollection = archiveCollection;
            Config = config;
            Map = map;
            BspTree = bspTree;
            Blockmap = new Blockmap(map);
            PhysicsManager = new PhysicsManager(bspTree, Blockmap); 
            EntityManager = new EntityManager(this, archiveCollection, bspTree, Blockmap, PhysicsManager, map);
        }

        public void Tick()
        {
            EntityManager.Players.Values.ForEach(player => player.Tick());
            
            EntityManager.Entities.ForEach(entity =>
            {
                entity.Tick();
                PhysicsManager.Move(entity);
            });
            
            Gametick++;
        }
    }
}