using Helion.Geometry;
using Helion.Window;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.Surfaces;

public class GLDefaultSurface
{
    private readonly IWindow m_window;
    private bool m_disposed;
    
    public Dimension Dimension => m_window.Dimension;

    public GLDefaultSurface(IWindow window)
    {
        m_window = window;
    }

    public void Bind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }
}