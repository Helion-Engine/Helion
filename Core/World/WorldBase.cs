using Helion.Maps;
using Helion.Projects;
using Helion.World.Geometry;

namespace Helion.World
{
    public abstract class WorldBase
    {
        public int Gametick { get; private set; } = 0;
        public Map Map;
        public BspTree BspTree;
        protected Project Project;

        protected WorldBase(Project project, Map map, BspTree bspTree)
        {
            Project = project;
            Map = map;
            BspTree = bspTree;
        }

        public virtual void Tick()
        {
            Gametick++;
        }
    }
}
