using Helion.Geometry;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Framebuffer;

public class PlaneClipFrameBuffer : IDisposable
{
    private int m_framebuffer;
    private int m_texture;
    private int m_clearBufferIndex;
    private Dimension m_dimension;
    private GLTexture2D? m_depthTexture;
    private bool m_ownsDepthTexture;

    public void CreateOrUpdate(string name, Dimension dimension, GLFramebuffer? colorFramebuffer, bool forceCreate)
    {
        if (!ShouldRecreate(dimension, forceCreate))
            return;

        DeleteData();

        m_dimension = dimension;
        var width = m_dimension.Width;
        var height = m_dimension.Height;

        m_texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, m_texture);
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, m_texture, $"{name} Texture");
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, width, height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        int depthTextureTarget;
        if (colorFramebuffer == null)
        {
            m_ownsDepthTexture = true;
            m_clearBufferIndex = 0;
            m_depthTexture = new GLTexture2D($"{name} Depth Stencil Attachment", m_dimension);
            m_depthTexture.Bind();
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth32fStencil8, width, height, 0, PixelFormat.DepthStencil, PixelType.Float32UnsignedInt248Rev, IntPtr.Zero);
            m_depthTexture.Unbind();
            depthTextureTarget = m_depthTexture.Name;
        }
        else
        {
            if (colorFramebuffer.DepthTexture == null)
                throw new Exception("Framebuffer must have a depth texture");
            m_clearBufferIndex = 1;
            depthTextureTarget = colorFramebuffer.DepthTexture.Name;
        }

        m_framebuffer = GL.GenFramebuffer();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Framebuffer, m_framebuffer, $"{name} Framebuffer");
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_framebuffer);

        if (colorFramebuffer == null)
        {
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, m_texture, 0);
        }
        else
        {
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorFramebuffer.ColorAttachment0.Name, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, m_texture, 0);
        }

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, depthTextureTarget, 0);

        if (colorFramebuffer == null)
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        else
            GL.DrawBuffers(2, [DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1]);

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
            throw new Exception("Failed to complete PlaneClip framebuffer");
    }

    private bool ShouldRecreate(Dimension dimension, bool force)
    {
        if (force)
            return true;

        if (m_framebuffer != 0 && dimension.Width == m_dimension.Width && dimension.Height == m_dimension.Height)
            return false;

        return true;
    }

    public unsafe void Clear()
    {
        if (m_ownsDepthTexture)
            GL.Clear(ClearBufferMask.DepthBufferBit);

        var clear = stackalloc float[3] { -1e30f, 1e30f, -1 };
        GL.ClearBuffer(ClearBuffer.Color, m_clearBufferIndex, clear);

        if (m_ownsDepthTexture)
            GL.Clear(ClearBufferMask.DepthBufferBit);
    }

    public static unsafe void StartRender()
    {
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
    }

    public void BindPlaneTexture(TextureUnit textureUnit)
    {
        GL.ActiveTexture(textureUnit);
        GL.BindTexture(TextureTarget.Texture2D, m_texture);
    }

    public void BindFrameBuffer()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_framebuffer);
    }

    public static void UnbindFrameBuffer()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        DeleteData();
    }

    private void DeleteData()
    {
        if (m_framebuffer != 0)
        {
            m_depthTexture?.Dispose();
            GL.DeleteTexture(m_texture);
            GL.DeleteFramebuffer(m_framebuffer);
            m_framebuffer = 0;
            m_depthTexture = null;
        }
    }
}
