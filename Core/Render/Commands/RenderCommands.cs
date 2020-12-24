using System.Collections.Generic;
using System.Drawing;
using Helion.Graphics.String;
using Helion.Render.Commands.Align;
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
    public class RenderCommands
    {
        public readonly Config Config;
        public readonly Dimension WindowDimension;
        public readonly IImageDrawInfoProvider ImageDrawInfoProvider;
        public readonly FpsTracker FpsTracker;
        public ResolutionInfo ResolutionInfo { get; private set; }
        private readonly List<IRenderCommand> m_commands = new();

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
            Rectangle drawArea = new(left, top, width, height);
            DrawImageCommand cmd = new(textureName, drawArea, color, alpha);
            m_commands.Add(cmd);
        }

        public void FillRect(Rectangle rectangle, Color color, float alpha)
        {
            m_commands.Add(new DrawShapeCommand(rectangle, color, alpha));
        }

        public void DrawText(ColoredString text, string font, int fontSize, int x, int y, int width, int height,
            TextAlignment textAlign, float alpha)
        {
            m_commands.Add(new DrawTextCommand(text, font, fontSize, x, y, width, height, textAlign, alpha));
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

        public void SetVirtualResolution(int width, int height, bool stretchIfWidescreen = false,
            bool centerIfStretched = false)
        {
            Dimension dimension = new Dimension(width, height);
            ResolutionInfo info = new()
            {
                VirtualDimensions = dimension,
                StretchIfWidescreen = stretchIfWidescreen,
                CenterIfWidescreen = centerIfStretched
            };
            SetVirtualResolution(info);
        }

        public void SetVirtualResolution(ResolutionInfo resolutionInfo)
        {
            ResolutionInfo = resolutionInfo;
        }

        public void UseNativeResolution()
        {
            ResolutionInfo = new ResolutionInfo { VirtualDimensions = WindowDimension };
        }

        public int GetFontHeight(string fontName) => ImageDrawInfoProvider.GetFontHeight(fontName);

        public IReadOnlyList<IRenderCommand> GetCommands() => m_commands.AsReadOnly();
    }
}