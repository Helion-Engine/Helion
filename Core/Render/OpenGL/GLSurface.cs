using System;
using Helion.Geometry;
using Helion.Render.Common.Renderers;
using Helion.Window;

namespace Helion.Render.OpenGL;

public class GLSurface : IRenderableSurface
{
    public string Name => IRenderableSurface.DefaultName;
    public Dimension Dimension => m_window.Dimension;
    private readonly IWindow m_window;
    private readonly GLRenderer m_renderer;
    private readonly GLRenderableSurfaceContext ctx;

    public GLSurface(IWindow window, GLRenderer renderer)
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
