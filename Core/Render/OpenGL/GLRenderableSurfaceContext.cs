using System;
using Helion.Geometry.Boxes;
using Helion.Graphics;
using Helion.Render.Common.Context;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL.Commands;
using Helion.Window;

namespace Helion.Render.OpenGL;

public class GLRenderableSurfaceContext : IRenderableSurfaceContext
{
    public IRenderableSurface Surface { get; }
    public readonly RenderCommands Commands;
    private readonly IWindow m_window;
    private readonly Renderer m_renderer;
    private readonly GLHudRenderContext m_hudRenderContext;
    private readonly GLWorldRenderContext m_worldRenderContext;
    private Box2I m_viewport;

    internal GLRenderableSurfaceContext(Renderer renderer, GLSurface surface)
    {
        m_renderer = renderer;
        Surface = surface;
        Commands = new(renderer.m_config, renderer.RenderDimension, renderer.Window.Dimension, renderer.DrawInfo, renderer.m_fpsTracker);
        m_hudRenderContext = new(renderer.m_archiveCollection, Commands, renderer.Textures);
        m_worldRenderContext = new(Commands);
        m_window = renderer.Window;
    }

    internal void Begin()
    {
        Commands.UpdateRenderDimension(m_renderer.RenderDimension, m_renderer.Window.Dimension);
        Commands.Begin();
    }

    public void Clear(Color color, bool depth, bool stencil)
    {
        Commands.Clear(color, depth, stencil);
    }

    public void ClearDepth()
    {
        Commands.ClearDepth();
    }

    public void ClearStencil()
    {
        // Not used.
    }

    public void Viewport(Box2I area)
    {
        m_viewport = area;
        Commands.Viewport(area.Dimension, area.Min);
    }

    public void Viewport(Box2I area, Action action)
    {
        Box2I oldViewport = m_viewport;

        Viewport(area);
        action();

        m_viewport = oldViewport;
    }

    public void Scissor(Box2I area)
    {
        // Not part of the old renderer.
    }

    public void Scissor(Box2I area, Action action)
    {
        // Not part of the old renderer.
    }

    public void DrawVirtualFrameBuffer()
    {
        Commands.DrawVirtualFrameBuffer();
    }

    public void Hud(HudRenderContext context, Action<IHudRenderContext> action)
    {
        m_hudRenderContext.Begin(context);
        action(m_hudRenderContext);
    }

    public void World(WorldRenderContext context, Action<IWorldRenderContext> action)
    {
        m_worldRenderContext.Begin(context);
        action(m_worldRenderContext);
    }

    public void Automap(WorldRenderContext context, Action<IWorldRenderContext> action)
    {
        m_worldRenderContext.Begin(context);
        action(m_worldRenderContext);
    }
}
