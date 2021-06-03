using Helion.Geometry;
using Helion.Geometry.Vectors;

namespace Helion.Render.Legacy.Commands.Types
{
    public record ViewportCommand : IRenderCommand
    {
        public readonly Dimension Dimension;
        public readonly Vec2I Offset;

        public ViewportCommand(Dimension dimension, Vec2I offset)
        {
            Dimension = dimension;
            Offset = offset;
        }
    }
}