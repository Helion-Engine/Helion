using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Helion.Graphics.String;
using Helion.Render.Commands.Alignment;
using Helion.Render.Commands.Types;
using Helion.Render.Shared;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using Helion.Util.Time;
using Helion.World;
using Helion.World.Entities;

namespace Helion.Render.Commands
{
    public class RenderCommands : IEnumerable<IRenderCommand>
    {
        public readonly Config Config;
        public readonly Dimension WindowDimension;
        public readonly IImageDrawInfoProvider ImageDrawInfoProvider;
        public readonly FpsTracker FpsTracker;
        public ResolutionInfo ResolutionInfo { get; private set; }
        private readonly List<IRenderCommand> m_commands = new();
        private Vec2D m_scale = Vec2D.One;
        private int m_centeringOffsetX;

        public RenderCommands(Config config, Dimension windowDimensions, IImageDrawInfoProvider imageDrawInfoProvider,
            FpsTracker fpsTracker)
        {
            Config = config;
            WindowDimension = windowDimensions;
            ResolutionInfo = new ResolutionInfo { VirtualDimensions = windowDimensions };
            ImageDrawInfoProvider = imageDrawInfoProvider;
            FpsTracker = fpsTracker;
        }

        public void Clear()
        {
            m_commands.Add(ClearRenderCommand.All());
        }

        public void ClearDepth()
        {
            m_commands.Add(ClearRenderCommand.DepthOnly());
        }

        public void DrawImage(CIString textureName, int left, int top, int width, int height, Color color,
            float alpha = 1.0f)
        {
            (int x, int y, int w, int h) = TranslateDimensions(left, top, width, height);
            Rectangle drawArea = new(x, y, w, h);
            DrawImageCommand cmd = new(textureName, drawArea, color, alpha);
            m_commands.Add(cmd);
        }

        public void FillRect(Rectangle rectangle, Color color, float alpha)
        {
            Rectangle transformedRectangle = TranslateDimensions(rectangle);
            DrawShapeCommand command = new(transformedRectangle, color, alpha);
            m_commands.Add(command);
        }

        public void DrawText(ColoredString text, string font, int fontSize, int left, int top, int width,
            int height, TextAlign textAlign, float alpha)
        {
            (int x, int y, int w, int h) = TranslateDimensions(left, top, width, height);
            DrawTextCommand command = new(text, font, fontSize, x, y, w, h, textAlign, alpha);
            m_commands.Add(command);
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

        public void SetVirtualResolution(int width, int height, ResolutionScale scale = ResolutionScale.None)
        {
            Dimension dimension = new Dimension(width, height);
            ResolutionInfo info = new() { VirtualDimensions = dimension, Scale = scale };
            SetVirtualResolution(info);
        }

        public void SetVirtualResolution(ResolutionInfo resolutionInfo)
        {
            ResolutionInfo = resolutionInfo;
            Dimension virtualDimension = resolutionInfo.VirtualDimensions;

            Vec2I windowDim = WindowDimension.ToVector();
            Vec2I virtualDim = virtualDimension.ToVector();
            m_scale = windowDim.ToDouble() / virtualDim.ToDouble();
            m_centeringOffsetX = 0;

            // By default we're stretching, but if we're centering, our values
            // have to change to accomodate a gutter if the aspect ratios are
            // different.
            if (resolutionInfo.Scale == ResolutionScale.Center)
            {
                // We only want to do centering if we will end up with gutters
                // on the side. This can only happen if the virtual dimension
                // has a smaller aspect ratio. We have to exit out if not since
                // it will cause weird overdrawing. This can happen in the case
                // of rendering something like 320x200 on a normal widescreen.
                if (WindowDimension.AspectRatio > virtualDimension.AspectRatio)
                {
                    m_scale.X = m_scale.Y;
                    m_centeringOffsetX = (WindowDimension.Width - (int)(virtualDimension.Width * m_scale.X)) / 2;
                }
            }
        }

        public void UseNativeResolution()
        {
            ResolutionInfo = new ResolutionInfo { VirtualDimensions = WindowDimension };
        }

        public int GetFontHeight(string fontName) => ImageDrawInfoProvider.GetFontHeight(fontName);

        public IEnumerator<IRenderCommand> GetEnumerator() => m_commands.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private (int x, int y, int w, int h) TranslateDimensions(int x, int y, int width, int height)
        {
            Rectangle drawArea = TranslateDimensions(new Rectangle(x, y, width, height));
            return (drawArea.X, drawArea.Y, drawArea.Width, drawArea.Height);
        }

        private Rectangle TranslateDimensions(Rectangle drawArea)
        {
            if (WindowDimension == ResolutionInfo.VirtualDimensions)
                return drawArea;

            Vec2I start = TranslatePoint(drawArea.X, drawArea.Y);
            Vec2I end = TranslatePoint(drawArea.X + drawArea.Width, drawArea.Y + drawArea.Height);
            Vec2I width = end - start;

            return new Rectangle(start.X + m_centeringOffsetX, start.Y, width.X, width.Y);
        }

        private Vec2I TranslatePoint(int x, int y) => (new Vec2D(x, y) * m_scale).ToInt();
    }
}