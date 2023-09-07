using Helion.Geometry;
using Helion.RenderNew.Renderers.Hud;
using Helion.RenderNew.Renderers.World;
using Helion.Window;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.Surfaces;

public class GLDefaultSurface : IGLSurface
{
    private readonly IWindow m_window;
    private bool m_disposed;
    
    public Dimension Dimension => m_window.Dimension;
    public HudRenderingContext Hud { get; }
    public WorldRenderingContext World { get; }

    public GLDefaultSurface(IWindow window, HudRenderingContext hud, WorldRenderingContext world)
    {
        m_window = window;
        Hud = hud;
        World = world;
    }

    public void Bind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }
    
    public void Unbind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }
}