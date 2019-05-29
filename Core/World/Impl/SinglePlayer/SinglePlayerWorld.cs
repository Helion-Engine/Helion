using Helion.Input;
using Helion.Maps;
using Helion.Projects;
using Helion.World.Geometry;

namespace Helion.World.Impl.SinglePlayer
{
    public class SinglePlayerWorld : WorldBase
    {
        protected SinglePlayerWorld(Project project, WorldGeometry geometry) : base(project, geometry)
        {
        }

        protected static SinglePlayerWorld? From(Project project, Map map)
        {
            WorldGeometry? geometry = WorldGeometry.From(map);
            if (geometry == null)
                return null;

            return new SinglePlayerWorld(project, geometry);
        }

        public void HandleTickInput(ConsumableInput tickInput)
        {
            // TODO
        }

        public void HandleFrameInput(ConsumableInput frameInput)
        {
            // TODO
        }
    }
}
