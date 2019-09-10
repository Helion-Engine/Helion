using System.Collections.Generic;
using System.Drawing;
using Helion.Graphics.String;
using Helion.Render.Commands.Types;
using Helion.Render.Shared;
using Helion.Render.Shared.Text;
using Helion.Util;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using Helion.World;
using Helion.World.Entities;

namespace Helion.Render.Commands
{
    public class RenderCommands
    {
        public readonly Dimension WindowDimension;
        private readonly List<IRenderCommand> m_commands = new List<IRenderCommand>();
        private readonly ITextDrawCalculator m_textDrawCalculator;

        public RenderCommands(Dimension windowDimensions, ITextDrawCalculator textDrawCalculator)
        {
            WindowDimension = windowDimensions;
            m_textDrawCalculator = textDrawCalculator;
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
        
        public void DrawImage(CIString textureName, Vec2I topLeft, Color color)
        {
            m_commands.Add(new DrawImageCommand(textureName, topLeft, color));
        }

        public void DrawImage(CIString textureName, Vec2I topLeft, float alpha)
        {
            m_commands.Add(new DrawImageCommand(textureName, topLeft, alpha));
        }
        
        public void DrawImage(CIString textureName, Vec2I topLeft, Color color, float alpha)
        {
            m_commands.Add(new DrawImageCommand(textureName, topLeft, color, alpha));
        }
        
        public void DrawImage(CIString textureName, int left, int top, int width, int height)
        {
            m_commands.Add(new DrawImageCommand(textureName, new Rectangle(left, top, width, height)));
        }
        
        public void DrawImage(CIString textureName, int left, int top, int width, int height, Color color)
        {
            m_commands.Add(new DrawImageCommand(textureName, new Rectangle(left, top, width, height), color));
        }
        
        public void DrawImage(CIString textureName, int left, int top, int width, int height, float alpha)
        {
            m_commands.Add(new DrawImageCommand(textureName, new Rectangle(left, top, width, height), alpha));
        }
        
        public void DrawImage(CIString textureName, int left, int top, int width, int height, Color color, float alpha)
        {
            m_commands.Add(new DrawImageCommand(textureName, new Rectangle(left, top, width, height), color, alpha));
        }

        public void FillRect(int left, int top, int width, int height, Color color)
        {
            FillRect(new Rectangle(left, top, width, height), color, 1.0f);
        }
        
        public void FillRect(int left, int top, int width, int height, Color color, float alpha)
        {
            FillRect(new Rectangle(left, top, width, height), color, alpha);
        }

        public void FillRect(Rectangle rectangle, Color color)
        {
            FillRect(rectangle, color, 1.0f);
        }

        public void FillRect(Rectangle rectangle, Color color, float alpha)
        {
            m_commands.Add(new DrawShapeCommand(rectangle, color, alpha));
        }

        public void DrawText(string text, string font, int x, int y)
        {
            DrawText(RGBColoredStringDecoder.Decode(text), font, x, y, 1.0f);
        }
        
        public void DrawText(string text, string font, int x, int y, out Rectangle drawArea)
        {
            DrawText(RGBColoredStringDecoder.Decode(text), font, x, y, 1.0f, out drawArea);
        }
        
        public void DrawText(ColoredString text, string font, int x, int y)
        {
            DrawText(text, font, x, y, 1.0f);
        }
        
        public void DrawText(ColoredString text, string font, int x, int y, out Rectangle drawArea)
        {
            DrawText(text, font, x, y, 1.0f, out drawArea);
        }
        
        public void DrawText(ColoredString text, string font, int x, int y, float alpha)
        {
            Vec2I topLeft = new Vec2I(x, y);
            m_commands.Add(new DrawTextCommand(text, font, topLeft, alpha, null));
        }
        
        public void DrawText(ColoredString text, string font, int x, int y, float alpha, out Rectangle drawArea)
        {
            Vec2I topLeft = new Vec2I(x, y);
            m_commands.Add(new DrawTextCommand(text, font, topLeft, alpha, null));
            
            drawArea = m_textDrawCalculator.GetDrawArea(text, font, topLeft);
        }

        public void DrawWorld(WorldBase world, Camera camera, int gametick, float fraction, Entity viewerEntity)
        {
            m_commands.Add(new DrawWorldCommand(world, camera, gametick, fraction, viewerEntity));
        }

        public void Viewport(Dimension dimension)
        {
            m_commands.Add(new ViewportCommand(dimension));
        }

        public void Viewport(Dimension dimension, Vec2I offset)
        {
            m_commands.Add(new ViewportCommand(dimension, offset));
        }

        public int GetFontHeight(string fontName) => m_textDrawCalculator.GetFontHeight(fontName);
        
        public IReadOnlyList<IRenderCommand> GetCommands() => m_commands.AsReadOnly();
    }
}