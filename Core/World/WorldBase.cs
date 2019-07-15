using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
using Helion.World.Geometry;

namespace Helion.World
{
    public abstract class WorldBase
    {
        public int Gametick { get; private set; }
        public readonly Map Map;
        public readonly BspTree BspTree;
        protected readonly ArchiveCollection ArchiveCollection;
        protected readonly Config Config;

        protected WorldBase(Config config, ArchiveCollection archiveCollection, Map map, BspTree bspTree)
        {
            ArchiveCollection = archiveCollection;
            Config = config;
            Map = map;
            BspTree = bspTree;
        }

        public virtual void Tick()
        {
            Gametick++;
        }
    }
}
