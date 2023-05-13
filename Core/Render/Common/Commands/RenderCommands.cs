using System.Collections;
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
    Viewport
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
    private Dimension m_windowDimensions;
    private Vec2D m_scale = Vec2D.One;
    private int m_centeringOffsetX;
    public List<RenderCommand> Commands = new();
    public List<ClearRenderCommand> ClearCommands = new();
    public List<DrawWorldCommand> WorldCommands = new();
    public List<DrawImageCommand> ImageCommands = new();
    public List<ViewportCommand> ViewportCommands = new();
    public List<DrawTextCommand> TextCommands = new();
    public List<DrawShapeCommand> ShapeCommands = new();

    public Dimension WindowDimension => m_windowDimensions;

    public RenderCommands(IConfig config, Dimension windowDimensions, IImageDrawInfoProvider imageDrawInfoProvider,
        FpsTracker fpsTracker)
    {
        Config = config;
        m_windowDimensions = windowDimensions;
        ResolutionInfo = new ResolutionInfo { VirtualDimensions = windowDimensions };
        ImageDrawInfoProvider = imageDrawInfoProvider;
        FpsTracker = fpsTracker;
    }

    public void UpdateRenderDimension(Dimension dimension)
    {
        m_windowDimensions = dimension;
        ResolutionInfo = new ResolutionInfo { VirtualDimensions = dimension };
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

        ResolutionInfo = new ResolutionInfo { VirtualDimensions = m_windowDimensions };
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
        float alpha = 1.0f, bool drawInvul = false)
    {
        ImageBox2I drawArea = TranslateDoomImageDimensions(left, top, width, height);
        DrawImageCommand cmd = new(textureName, drawArea, color, alpha, drawInvul);
        Commands.Add(new RenderCommand(RenderCommandType.Image, ImageCommands.Count));
        ImageCommands.Add(cmd);
    }

    public void FillRect(ImageBox2I rectangle, Color color, float alpha)
    {
        ImageBox2I transformedRectangle = TranslateDimensions(rectangle);
        DrawShapeCommand command = new(transformedRectangle, color, alpha);
        Commands.Add(new RenderCommand(RenderCommandType.Shape, ShapeCommands.Count));
        ShapeCommands.Add(command);
    }

    public void DrawText(RenderableString str, int left, int top, float alpha)
    {
        ImageBox2I drawArea = TranslateDimensions(left, top, str.DrawArea);
        DrawTextCommand command = new(str, drawArea, alpha);
        Commands.Add(new RenderCommand(RenderCommandType.Text, TextCommands.Count));
        TextCommands.Add(command);
    }

    public void DrawWorld(IWorld world, OldCamera camera, int gametick, float fraction, Entity viewerEntity, bool drawAutomap,
        Vec2I automapOffset, double automapScale)
    {
        Commands.Add(new RenderCommand(RenderCommandType.World, WorldCommands.Count));
        WorldCommands.Add(new DrawWorldCommand(world, camera, gametick, fraction, viewerEntity, drawAutomap, automapOffset, automapScale));
    }

    public void Viewport(Dimension dimension, Vec2I? offset = null)
    {
        Commands.Add(new RenderCommand(RenderCommandType.Viewport, ViewportCommands.Count));
        ViewportCommands.Add(new ViewportCommand(dimension, offset ?? Vec2I.Zero));
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

        double viewWidth = WindowDimension.Height * resolutionInfo.AspectRatio;
        double scaleWidth = viewWidth / resolutionInfo.VirtualDimensions.Width;
        double scaleHeight = WindowDimension.Height / (double)resolutionInfo.VirtualDimensions.Height;
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
            m_centeringOffsetX = (WindowDimension.Width - (int)(resolutionInfo.VirtualDimensions.Width * m_scale.X)) / 2;
        }
    }

    private ImageBox2I TranslateDimensions(int x, int y, Dimension dimension)
    {
        return TranslateDimensions(new ImageBox2I(x, y, x + dimension.Width, y + dimension.Height));
    }

    private ImageBox2I TranslateDoomImageDimensions(int x, int y, int width, int height)
    {
        if (WindowDimension == ResolutionInfo.VirtualDimensions)
            return new ImageBox2I(x, y, x + width, y + height);

        ImageBox2I drawLocation = new ImageBox2I(x, y, x + width, y + height);
        drawLocation = TranslateDimensions(drawLocation);
        return drawLocation;
    }

    private ImageBox2I TranslateDimensions(ImageBox2I drawArea)
    {
        if (WindowDimension == ResolutionInfo.VirtualDimensions)
            return drawArea;

        int offsetX = m_centeringOffsetX;
        Vec2I start = TranslatePoint(drawArea.Left, drawArea.Top);
        Vec2I end = TranslatePoint(drawArea.Right, drawArea.Bottom);
        return new ImageBox2I(start.X + offsetX, start.Y, end.X + offsetX, end.Y);
    }

    private Vec2I TranslatePoint(int x, int y) => (new Vec2D(x, y) * m_scale).Int;
}
