using Helion.Input;
using Helion.Maps;
using Helion.Projects;
using Helion.Render.Shared;
using Helion.World.Geometry;
using System.Numerics;

namespace Helion.World.Impl.SinglePlayer
{
    public class SinglePlayerWorld : WorldBase
    {
        public Camera Camera { get; } = new Camera(new Vector3(-64, 1584, 90), 0.0f);

        private SinglePlayerWorld(Project project, Map map, BspTree bspTree) : base(project, map, bspTree)
        {
        }

        public static SinglePlayerWorld? Create(Project project, Map map)
        {
            BspTree? bspTree = BspTree.Create(map);
            if (bspTree == null)
                return null;

            return new SinglePlayerWorld(project, map, bspTree);
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
