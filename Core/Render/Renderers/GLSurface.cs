using System;
using Helion;
using Helion.Geometry;
using Helion.Render;
using Helion.Render.Common.Renderers;
using Helion.Render.Legacy;
using Helion.Render.Renderers;
using Helion.Window;

namespace Helion.Render.Renderers;

public class GLSurface : IRenderableSurface
{
    public string Name => IRenderableSurface.DefaultName;
    public Dimension Dimension => m_window.Dimension;
    private readonly IWindow m_window;
    private readonly Renderer m_renderer;
    private readonly GLRenderableSurfaceContext ctx;

    public GLSurface(IWindow window, Renderer renderer)
    {
        m_window = window;
        m_renderer = renderer;
        ctx = new GLRenderableSurfaceContext(renderer, this);
    }

    public void Render(Action<IRenderableSurfaceContext> action)
    {
        ctx.Begin();
        action(ctx);
        m_renderer.Render(ctx.Commands);
    }

    public void Dispose()
    {
        // Nothing to dispose of.
        GC.SuppressFinalize(this);
    }
}
