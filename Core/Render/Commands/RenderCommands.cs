using System.Collections.Generic;
using System.Drawing;
using Helion.Render.Commands.Types;
using Helion.Render.Shared;
using Helion.Util;
using Helion.Util.Geometry;
using Helion.World;

namespace Helion.Render.Commands
{
    public class RenderCommands
    {
        private readonly List<IRenderCommand> m_commands = new List<IRenderCommand>();
        private Dimension m_windowDimension;

        public RenderCommands(Dimension windowDimensions)
        {
            m_windowDimension = windowDimensions;
        }
        
        public void Clear()
        {
            m_commands.Add(ClearRenderCommand.All());
        }

        public void DrawImage(CIString textureName, Rectangle drawArea)
        {
            m_commands.Add(new DrawImageCommand(textureName, drawArea));
        }
        
        public void DrawImage(CIString textureName, Rectangle drawArea, float alpha)
        {
            m_commands.Add(new DrawImageCommand(textureName, drawArea, alpha));
        }
        
        public void DrawWorld(WorldBase world, Camera camera, int gametick, float fraction)
        {
            m_commands.Add(new DrawWorldCommand(world, camera, gametick, fraction));
        }

        public void Viewport(Dimension dimension)
        {
            m_commands.Add(new ViewportCommand(dimension));
        }

        public void Viewport(Dimension dimension, Vec2I offset)
        {
            m_commands.Add(new ViewportCommand(dimension, offset));
        }

        public IReadOnlyList<IRenderCommand> GetCommands() => m_commands.AsReadOnly();
    }
}