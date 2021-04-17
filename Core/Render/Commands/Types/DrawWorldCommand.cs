using Helion.Render.Shared;
using Helion.World;
using Helion.World.Entities;

namespace Helion.Render.Commands.Types
{
    public record DrawWorldCommand : IRenderCommand
    {
        public readonly WorldBase World;
        public readonly Camera Camera;
        public readonly int Gametick;
        public readonly float GametickFraction;
        public readonly Entity ViewerEntity;
        public readonly bool DrawAutomap;

        public DrawWorldCommand(WorldBase world, Camera camera, int gametick, float gametickFraction,
            Entity viewerEntity, bool drawAutomap)
        {
            World = world;
            Camera = camera;
            Gametick = gametick;
            GametickFraction = gametickFraction;
            ViewerEntity = viewerEntity;
            DrawAutomap = drawAutomap;
        }
    }
}