using System.Collections.Generic;
using System.Runtime.InteropServices;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Graphics.Geometry;
using Helion.Render.Common.Enums;
using Helion.Render.OpenGL.Commands.Types;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Util.Configs;
using Helion.Util.Timing;
using Helion.World;
using Helion.World.Entities;

namespace Helion.Render.OpenGL.Commands;

public enum RenderCommandType
{
    Clear,
    World,
    Image,
    Text,
    Shape,
    Viewport,
    DrawVirtualFrameBuffer,
    Automap
}

[StructLayout(LayoutKind.Sequential)]
public struct RenderCommand
{
    public RenderCommand(RenderCommandType type, int index)
    {
        Type = type;
        Index = index;
    }

    public RenderCommandType Type;
    public int Index;
};

public class RenderCommands
{
    public readonly IConfig Config;
    public readonly IImageDrawInfoProvider ImageDrawInfoProvider;
    public readonly FpsTracker FpsTracker;
    public ResolutionInfo ResolutionInfo { get; private set; }
    private Dimension m_renderDimensions;
    private Dimension m_windowDimensions;
    private Vec2D m_scale = Vec2D.One;
    private int m_centeringOffsetX;
    private float m_alpha = 1;
    private Vec2I m_offset = Vec2I.Zero;
    public List<RenderCommand> Commands = new();
    public List<DrawWorldCommand> AutomapCommands = new();
    public List<ClearRenderCommand> ClearCommands = new();
    public List<DrawWorldCommand> WorldCommands = new();
    public List<DrawImageCommand> ImageCommands = new();
    public List<ViewportCommand> ViewportCommands = new();
    public List<DrawTextCommand> TextCommands = new();
    public List<DrawShapeCommand> ShapeCommands = new();

    public Dimension RenderDimension => m_renderDimensions;
    public Dimension WindowDimension => m_windowDimensions;
    public Vec2I Offset => m_offset;

    public RenderCommands(IConfig config, Dimension renderDimensions, Dimension windowDimensions, IImageDrawInfoProvider imageDrawInfoProvider,
        FpsTracker fpsTracker)
    {
        Config = config;
        m_renderDimensions = renderDimensions;
        m_windowDimensions = windowDimensions;
        ResolutionInfo = new ResolutionInfo { VirtualDimensions = renderDimensions };
        ImageDrawInfoProvider = imageDrawInfoProvider;
        FpsTracker = fpsTracker;
    }

    public void UpdateRenderDimension(Dimension renderDimension, Dimension windowDimension)
    {
        m_renderDimensions = renderDimension;
        m_windowDimensions = windowDimension;
        ResolutionInfo = new ResolutionInfo { VirtualDimensions = renderDimension };
    }

    public void Begin()
    {
        Commands.Clear();
        ClearCommands.Clear();
        WorldCommands.Clear();
        ImageCommands.Clear();
        ViewportCommands.Clear();
        TextCommands.Clear();
        ShapeCommands.Clear();
        AutomapCommands.Clear();

        ResolutionInfo = new ResolutionInfo { VirtualDimensions = RenderDimension };
        m_scale = Vec2D.One;
        m_centeringOffsetX = 0;
    }

    public void Clear(Color color, bool depth = false, bool stencil = false)
    {
        Commands.Add(new RenderCommand(RenderCommandType.Clear, ClearCommands.Count));
        ClearCommands.Add(new ClearRenderCommand(true, depth, stencil, color));
    }

    public void ClearDepth()
    {
        Commands.Add(new RenderCommand(RenderCommandType.Clear, ClearCommands.Count));
        ClearCommands.Add(ClearRenderCommand.DepthOnly());
    }

    public void DrawImage(string textureName, int left, int top, int width, int height, Color color,
        float alpha = 1.0f, bool drawInvul = false, bool drawFuzz = false, bool drawColorMap = true)
    {
        ImageBox2I drawArea = TranslateDoomImageDimensions(left, top, width, height);
        DrawImageCommand cmd = new(textureName, drawArea, color, alpha * m_alpha, drawInvul, drawFuzz, drawColorMap);
        Commands.Add(new RenderCommand(RenderCommandType.Image, ImageCommands.Count));
        ImageCommands.Add(cmd);
    }

    public void FillRect(ImageBox2I rectangle, Color color, float alpha)
    {
        ImageBox2I transformedRectangle = TranslateDimensions(rectangle);
        DrawShapeCommand command = new(transformedRectangle, color, alpha * m_alpha);
        Commands.Add(new RenderCommand(RenderCommandType.Shape, ShapeCommands.Count));
        ShapeCommands.Add(command);
    }

    public void DrawText(RenderableString str, int left, int top, float alpha, bool drawColorMap)
    {
        ImageBox2I drawArea = TranslateDimensions(left, top, str.DrawArea);
        DrawTextCommand command = new(str, drawArea, alpha * m_alpha, drawColorMap);
        Commands.Add(new RenderCommand(RenderCommandType.Text, TextCommands.Count));
        TextCommands.Add(command);
    }

    public void DrawWorld(IWorld world, OldCamera camera, int gametick, float fraction, Entity viewerEntity, bool drawAutomap,
        Vec2I automapOffset, double automapScale)
    {
        Commands.Add(new RenderCommand(RenderCommandType.World, WorldCommands.Count));
        WorldCommands.Add(new DrawWorldCommand(world, camera, gametick, fraction, viewerEntity, drawAutomap, automapOffset, automapScale));
    }

    public void DrawAutomap(IWorld world, OldCamera camera, int gametick, float fraction, Entity viewerEntity, bool drawAutomap,
        Vec2I automapOffset, double automapScale)
    {
        Commands.Add(new RenderCommand(RenderCommandType.Automap, AutomapCommands.Count));
        AutomapCommands.Add(new DrawWorldCommand(world, camera, gametick, fraction, viewerEntity, drawAutomap, automapOffset, automapScale));
    }

    public void Viewport(Dimension dimension, Vec2I? offset = null)
    {
        Commands.Add(new RenderCommand(RenderCommandType.Viewport, ViewportCommands.Count));
        ViewportCommands.Add(new ViewportCommand(dimension, offset ?? Vec2I.Zero));
    }

    public void DrawVirtualFrameBuffer()
    {
        Commands.Add(new RenderCommand(RenderCommandType.DrawVirtualFrameBuffer, 0));
    }

    /// <summary>
    /// Sets a virtual resolution to draw with.
    /// </summary>
    /// <param name="width">The virtual window width.</param>
    /// <param name="height">The virtual window height.</param>
    /// <param name="scale">How to scale drawing.</param>
    public void SetVirtualResolution(int width, int height, ResolutionScale scale = ResolutionScale.None)
    {
        Dimension dimension = new Dimension(width, height);
        ResolutionInfo info = new() { VirtualDimensions = dimension, Scale = scale };
        SetVirtualResolution(info);
    }

    /// <summary>
    /// Sets a virtual resolution to draw with.
    /// </summary>
    /// <param name="resolutionInfo">Resolution parameters.</param>
    public void SetVirtualResolution(ResolutionInfo resolutionInfo)
    {
        ResolutionInfo = resolutionInfo;

        double viewWidth = RenderDimension.Height * resolutionInfo.AspectRatio;
        double scaleWidth = viewWidth / resolutionInfo.VirtualDimensions.Width;
        double scaleHeight = RenderDimension.Height / (double)resolutionInfo.VirtualDimensions.Height;
        m_scale = new Vec2D(scaleWidth, scaleHeight);

        m_centeringOffsetX = 0;

        // By default we're stretching, but if we're centering, our values
        // have to change to accomodate a gutter if the aspect ratios are
        // different.
        if (resolutionInfo.Scale == ResolutionScale.Center)
        {
            // We only want to do centering if we will end up with gutters
            // on the side. This can only happen if the virtual dimension
            // has a smaller aspect ratio. We have to exit out if not since
            // it will cause weird overdrawing otherwise.
            m_centeringOffsetX = (RenderDimension.Width - (int)(resolutionInfo.VirtualDimensions.Width * m_scale.X)) / 2;
        }
    }

    public void SetAlpha(float alpha) => m_alpha = alpha;

    public void SetOffset(Vec2I offset) => m_offset = offset;

    public void AddOffset(Vec2I offset) => m_offset += offset;

    private ImageBox2I TranslateDimensions(int x, int y, Dimension dimension)
    {
        return TranslateDimensions(new ImageBox2I(x, y, x + dimension.Width, y + dimension.Height));
    }

    private ImageBox2I TranslateDoomImageDimensions(int x, int y, int width, int height)
    {
        x += m_offset.X;
        y += m_offset.Y;
        if (RenderDimension == ResolutionInfo.VirtualDimensions)
            return new ImageBox2I(x, y, x + width, y + height);

        ImageBox2I drawLocation = new ImageBox2I(x, y, x + width, y + height);
        drawLocation = TranslateDimensions(drawLocation);
        return drawLocation;
    }

    private ImageBox2I TranslateDimensions(ImageBox2I drawArea)
    {
        drawArea = new(drawArea.Min.X + m_offset.X, drawArea.Min.Y + m_offset.Y,
            drawArea.Max.X + m_offset.X, drawArea.Max.Y + m_offset.Y);
        if (RenderDimension == ResolutionInfo.VirtualDimensions)
            return drawArea;

        int offsetX = m_centeringOffsetX;
        Vec2I start = TranslatePoint(drawArea.Left, drawArea.Top);
        Vec2I end = TranslatePoint(drawArea.Right, drawArea.Bottom);
        return new ImageBox2I(start.X + offsetX, start.Y, end.X + offsetX, end.Y);
    }

    private Vec2I TranslatePoint(int x, int y) => (new Vec2D(x, y) * m_scale).Int;
}
