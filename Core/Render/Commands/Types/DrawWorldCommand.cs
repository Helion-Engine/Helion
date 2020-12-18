using Helion.Render.Shared;
using Helion.World;
using Helion.World.Entities;
using Entity = Helion.Worlds.Entities.Entity;

namespace Helion.Render.Commands.Types
{
    public readonly struct DrawWorldCommand : IRenderCommand
    {
        public readonly Worlds.World World;
        public readonly Camera Camera;
        public readonly int Gametick;
        public readonly float GametickFraction;
        public readonly Entity ViewerEntity;

        public DrawWorldCommand(Worlds.World world, Camera camera, int gametick, float gametickFraction,
            Entity viewerEntity)
        {
            World = world;
            Camera = camera;
            Gametick = gametick;
            GametickFraction = gametickFraction;
            ViewerEntity = viewerEntity;
        }
    }
}