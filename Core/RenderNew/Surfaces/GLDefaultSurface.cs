using Helion.Geometry;
using Helion.RenderNew.Renderers.Hud;
using Helion.RenderNew.Renderers.World;
using Helion.Window;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.Surfaces;

public class GLDefaultSurface
{
    public readonly HudRenderingContext HudRenderer;
    public readonly WorldRenderingContext WorldRenderer;
    private readonly IWindow m_window;
    private bool m_disposed;
    
    public Dimension Dimension => m_window.Dimension;

    public GLDefaultSurface(IWindow window, HudRenderingContext hudRenderer, WorldRenderingContext worldRenderer)
    {
        m_window = window;
        HudRenderer = hudRenderer;
        WorldRenderer = worldRenderer;
    }

    public void Bind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }
}