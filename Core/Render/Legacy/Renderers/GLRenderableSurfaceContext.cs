using System;
using System.Drawing;
using Helion;
using Helion.Geometry.Boxes;
using Helion.Render;
using Helion.Render.Common.Context;
using Helion.Render.Common.Renderers;
using Helion.Render.Legacy;
using Helion.Render.Legacy.Commands;
using Helion.Render.Legacy.Renderers;
using Helion.Window;

namespace Helion.Render.Legacy.Renderers;

public class GLRenderableSurfaceContext : IRenderableSurfaceContext
{
    public IRenderableSurface Surface { get; }
    public readonly RenderCommands Commands;
    private readonly IWindow m_window;
    private readonly GLHudRenderContext m_hudRenderContext;
    private readonly GLWorldRenderContext m_worldRenderContext;
    private Box2I m_viewport;

    internal GLRenderableSurfaceContext(Renderer renderer, GLSurface surface)
    {
        Surface = surface;
        Commands = new RenderCommands(renderer.m_config, renderer.Window.Dimension,
            renderer.ImageDrawInfoProvider, renderer.m_fpsTracker);

        m_hudRenderContext = new GLHudRenderContext(renderer.m_archiveCollection, Commands,
            renderer.Textures);
        m_worldRenderContext = new GLWorldRenderContext(Commands);
        m_window = renderer.Window;
    }

    internal void Begin()
    {
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
}
