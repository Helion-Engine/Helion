using GlmSharp;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Framebuffer;
using Helion.Render.OpenGL.Vertex;
using Helion.Window;
using Helion.World;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Renderers;

public class TransitionRenderer : IDisposable
{
    private readonly IWindow m_window;
    private readonly StaticVertexBuffer<FramebufferVertex> m_vbo = new("Transition");
    private readonly VertexArrayObject m_vao = new("Transition");

    private readonly FadeTransitionProgram m_fadeProgram = new();
    private readonly MeltTransitionProgram m_meltProgram = new();
    private readonly NoTransitionProgram m_noProgram = new();
    private TransitionProgram? m_program;
    /// <summary>
    /// The screen buffer to transition from.
    /// </summary>
    private GLFramebuffer m_startBuffer;
    private bool m_disposed;

    public TransitionRenderer(IWindow window)
    {
        m_window = window;
        m_startBuffer = GetNewFramebuffer();

        Attributes.BindAndApply(m_vbo, m_vao, m_fadeProgram.Attributes);
        Attributes.BindAndApply(m_vbo, m_vao, m_meltProgram.Attributes);
        Attributes.BindAndApply(m_vbo, m_vao, m_noProgram.Attributes);

        UploadVertices();
    }

    ~TransitionRenderer()
    {
        Dispose(false);
    }

    private GLFramebuffer GetNewFramebuffer() => new("Transition", m_window.ClientDimension, 1);

    public void UpdateFramebufferDimensionsIfNeeded()
    {
        if (m_startBuffer.Dimension != m_window.ClientDimension && m_window.ClientDimension.HasPositiveArea)
        {
            m_startBuffer.Dispose();
            m_startBuffer = GetNewFramebuffer();
        }
    }

    public void PrepareNewTransition(GLFramebuffer sourceBuffer, TransitionType type)
    {
        m_program = type switch
        {
            TransitionType.Fade => m_fadeProgram,
            TransitionType.Melt => m_meltProgram,
            // show the last framebuffer for a brief moment
            // so there's no flicker for very short loads
            _ => m_noProgram
        };

        sourceBuffer.BindRead();
        m_startBuffer.BindDraw();
        GL.BlitFramebuffer(0, 0, sourceBuffer.Dimension.Width, sourceBuffer.Dimension.Height,
            0, 0, m_startBuffer.Dimension.Width, m_startBuffer.Dimension.Height,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
        sourceBuffer.BindDraw();
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

    public void Render(GLFramebuffer targetBuffer, float progress)
    {
        if (m_program == null)
            return;

        m_startBuffer.BindRead();
        targetBuffer.BindDraw();
        GL.Viewport(0, 0, targetBuffer.Dimension.Width, targetBuffer.Dimension.Height);
        m_program.Bind();

        GL.ActiveTexture(BindTextures.BoundTexture);
        m_startBuffer.ColorAttachment0.Bind();
        if (m_program is MeltTransitionProgram meltProgram)
        {
            // the melt shader uses ticks, so convert [0,1] to [0,42] ticks
            float loopElapsedTicks = progress * 42;
            // TODO: would be nice here to align strips with the virtual res
            meltProgram.SetUniforms(BindTextures.BoundTexture, mat4.Identity, loopElapsedTicks, targetBuffer.Dimension.Width / 4);
        }
        else if (m_program is FadeTransitionProgram fadeProgram)
            fadeProgram.SetUniforms(BindTextures.BoundTexture, mat4.Identity, progress);
        else
            m_program.SetUniforms(BindTextures.BoundTexture, mat4.Identity);

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
        m_meltProgram.Dispose();
        m_fadeProgram.Dispose();
        m_noProgram.Dispose();
        m_startBuffer.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
