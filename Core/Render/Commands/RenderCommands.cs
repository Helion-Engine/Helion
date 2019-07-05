using Helion.Render.Commands.Types;
using Helion.Util.Geometry;
using System.Collections.Generic;
using Helion.Render.Shared;
using Helion.World;

namespace Helion.Render.Commands
{
    public class RenderCommands
    {
        private Dimension windowDimension;
        private List<IRenderCommand> commands = new List<IRenderCommand>();

        public RenderCommands(Dimension windowDimensions)
        {
            windowDimension = windowDimensions;
        }
        
        public void Clear()
        {
            commands.Add(ClearRenderCommand.All());
        }

        public void DrawWorld(WorldBase world, Camera camera, int gametick, float fraction)
        {
            commands.Add(new DrawWorldCommand(world, camera, gametick, fraction));
        }

        public void Viewport(Dimension dimension)
        {
            commands.Add(new ViewportCommand(dimension));
        }

        public void Viewport(Dimension dimension, Vec2I offset)
        {
            commands.Add(new ViewportCommand(dimension, offset));
        }

        public IReadOnlyList<IRenderCommand> GetCommands() => commands.AsReadOnly();
    }
}