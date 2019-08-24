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
        public readonly Dimension WindowDimension;
        private readonly List<IRenderCommand> m_commands = new List<IRenderCommand>();

        public RenderCommands(Dimension windowDimensions)
        {
            WindowDimension = windowDimensions;
        }
        
        public void Clear()
        {
            m_commands.Add(ClearRenderCommand.All());
        }
        
        public void ClearDepth()
        {
            m_commands.Add(ClearRenderCommand.DepthOnly());
        }

        public void DrawImage(CIString textureName, Vec2I topLeft)
        {
            m_commands.Add(new DrawImageCommand(textureName, topLeft));
        }
        
        public void DrawImage(CIString textureName, int left, int top, int width, int height)
        {
            m_commands.Add(new DrawImageCommand(textureName, new Rectangle(left, top, width, height)));
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