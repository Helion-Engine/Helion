using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.World.Geometry;

namespace Helion.World
{
    public abstract class WorldBase
    {
        public int Gametick { get; private set; }
        public readonly Map Map;
        public readonly BspTree BspTree;
        protected readonly ArchiveCollection ArchiveCollection;

        protected WorldBase(ArchiveCollection archiveCollection, Map map, BspTree bspTree)
        {
            ArchiveCollection = archiveCollection;
            Map = map;
            BspTree = bspTree;
        }

        public virtual void Tick()
        {
            Gametick++;
        }
    }
}
