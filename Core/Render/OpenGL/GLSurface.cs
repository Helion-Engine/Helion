using System;
using Helion.Geometry;
using Helion.Render.Common.Renderers;
using Helion.Window;

namespace Helion.Render.OpenGL;

public class GLSurface : IRenderableSurface
{
    public string Name => IRenderableSurface.DefaultName;
    public Dimension Dimension => m_overrideDimension.Height > 0 ? m_overrideDimension : m_renderer.RenderDimension;
    private readonly IWindow m_window;
    private readonly Renderer m_renderer;
    private readonly GLRenderableSurfaceContext ctx;
    private Dimension m_overrideDimension;

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

    public void SetOverrideDimension(Dimension dimension)
    {
        m_overrideDimension = dimension;
    }

    public void ClearOverrideDimension()
    {
        m_overrideDimension = (0, 0);
    }
}
