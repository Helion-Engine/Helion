using GlmSharp;
using Helion.Graphics;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Framebuffer;
using Helion.Render.OpenGL.Vertex;
using Helion.Window;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Renderers;

public class BasicFramebufferRenderer : IDisposable
{
    private readonly IWindow m_window;
    private readonly StaticVertexBuffer<FramebufferVertex> m_vbo = new("Framebuffer");
    private readonly VertexArrayObject m_vao = new("Framebuffer");
    private readonly FramebufferProgram m_program = new();
    private bool m_disposed;

    public BasicFramebufferRenderer(IWindow window)
    {
        m_window = window;

        Attributes.BindAndApply(m_vbo, m_vao, m_program.Attributes);
        UploadVertices();
    }

    ~BasicFramebufferRenderer()
    {
        Dispose(false);
    }

    private void UploadVertices()
    {
        FramebufferVertex topLeft = new((-1, 1), (0, 1));
        FramebufferVertex topRight = new((1, 1), (1, 1));
        FramebufferVertex bottomLeft = new((-1, -1), (0, 0));
        FramebufferVertex bottomRight = new((1, -1), (1, 0));

        m_vbo.Bind();
        m_vbo.Add(topLeft, bottomLeft, topRight);
        m_vbo.Add(topRight, bottomLeft, bottomRight);
        m_vbo.Upload();
        m_vbo.Unbind();
    }

    public void Render(GLFramebuffer buffer)
    {
        (float a, float r, float g, float b) = Color.Black.Normalized;

        GL.Viewport(0, 0, m_window.Dimension.Width, m_window.Dimension.Height);
        GL.ClearColor(r, g, b, a);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        m_program.Bind();

        GL.ActiveTexture(TextureUnit.Texture0);
        buffer.Textures[0].Bind();
        m_program.BoundTexture(TextureUnit.Texture0);
        m_program.Mvp(mat4.Identity);

        m_vao.Bind();
        m_vbo.DrawArrays();
        m_vao.Unbind();

        m_program.Unbind();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        m_vbo.Dispose();
        m_vao.Dispose();
        m_program.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
