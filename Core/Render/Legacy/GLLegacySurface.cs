using System;
using Helion.Geometry;
using Helion.Render;
using Helion.Render.Common.Renderers;
using Helion.Window;

namespace Helion.Render.Legacy;

public class GLLegacySurface : IRenderableSurface
{
    public string Name => IRenderableSurface.DefaultName;
    public Dimension Dimension => m_window.Dimension;
    private readonly IWindow m_window;
    private readonly Renderer m_renderer;
    private readonly GLLegacyRenderableSurfaceContext ctx;

    public GLLegacySurface(IWindow window, Renderer renderer)
    {
        m_window = window;
        m_renderer = renderer;
        ctx = new GLLegacyRenderableSurfaceContext(renderer, this);
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
