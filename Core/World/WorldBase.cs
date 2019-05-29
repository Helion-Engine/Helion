using Helion.Projects;
using Helion.World.Geometry;

namespace Helion.World
{
    public abstract class WorldBase
    {
        public int Gametick { get; private set; }
        private Project project;
        private WorldGeometry geometry;

        protected WorldBase(Project projectForWorld, WorldGeometry worldGeometry)
        {
            project = projectForWorld;
            geometry = worldGeometry;
        }

        public virtual void Tick()
        {
            Gametick++;
        }
    }
}
