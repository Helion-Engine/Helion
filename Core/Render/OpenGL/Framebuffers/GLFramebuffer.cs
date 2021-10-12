using System;
using Helion.Geometry;
using Helion.Render.OpenGL.Capabilities;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Framebuffers;

/// <summary>
/// A representation of a non-default framebuffer.
/// </summary>
public class GLFramebuffer : IDisposable
{
    public readonly Dimension Dimension;
    private int m_name;
    private bool m_disposed;

    private GLFramebuffer(Dimension dimension)
    {
        Dimension = dimension;
        m_name = AllocateFramebuffer();
        // TODO: Set up texture
        // TODO: Verify
    }

    ~GLFramebuffer()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public static GLFramebuffer? Create(Dimension dimension)
    {
        return GLCapabilities.SupportsFramebufferObjects ? new GLFramebuffer(dimension) : null;
    }

    public void Bind()
    {
        PerformBind(m_name);
    }

    public void Unbind()
    {
        PerformBind(0);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        PerformDispose();
    }

    private void PerformDispose()
    {
        if (m_disposed)
            return;

        DeallocateFramebuffer(m_name);
        m_name = 0;

        m_disposed = true;
    }

    /// <summary>
    /// Will properly select the binding function based on the version of
    /// of OpenGL available.
    /// </summary>
    /// <param name="fbo">The framebuffer object index.</param>
    /// <param name="target">The framebuffer target.</param>
    /// <returns>True if it could bind, false if it could not due to a low
    /// opengl version.</returns>
    public static bool PerformBind(int fbo, FramebufferTarget target = FramebufferTarget.Framebuffer)
    {
        if (GLCapabilities.Extensions.Framebuffers.HasNativeSupport)
            GL.BindFramebuffer(target, fbo);
        else if (GLCapabilities.Extensions.Framebuffers.HasExtSupport)
            GL.Ext.BindFramebuffer(target, fbo);
        else
            return false;

        return true;
    }

    private static int AllocateFramebuffer()
    {
        if (GLCapabilities.Extensions.Framebuffers.HasNativeSupport)
            return GL.GenRenderbuffer();

        // This will explode if it doesn't exist. However code should never
        // be calling this function if there is no framebuffer support.
        return GL.Ext.GenRenderbuffer();
    }

    private static void DeallocateFramebuffer(int fbo)
    {
        if (GLCapabilities.Extensions.Framebuffers.HasNativeSupport)
            GL.DeleteFramebuffer(fbo);
        else if (GLCapabilities.Extensions.Framebuffers.HasExtSupport)
            GL.Ext.DeleteFramebuffer(fbo);
    }
}
