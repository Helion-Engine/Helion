using Helion.Render.Shared;
using Helion.World;

namespace Helion.Render.Commands.Types
{
    public readonly struct DrawWorldCommand : IRenderCommand
    {
        public readonly WorldBase World;
        public readonly Camera Camera;
        public readonly int Gametick;
        public readonly float GametickFraction;

        public DrawWorldCommand(WorldBase world, Camera camera, int gametick, float gametickFraction)
        {
            World = world;
            Camera = camera;
            Gametick = gametick;
            GametickFraction = gametickFraction;
        }
    }
}