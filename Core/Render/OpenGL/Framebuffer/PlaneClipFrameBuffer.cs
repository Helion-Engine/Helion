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
    private Dimension m_dimension;
    private GLTexture2D? m_depthTexture;

    public void CreateOrUpdate(Dimension dimension)
    {
        if (m_framebuffer != 0 && dimension.Width == m_dimension.Width && dimension.Height == m_dimension.Height)
            return;

        DeleteData();

        m_dimension = dimension;
        var width = m_dimension.Width;
        var height = m_dimension.Height;

        m_texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, m_texture);
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, m_texture, "PlaneClip Texture");
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rg32f, width, height, 0, PixelFormat.Rg, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        m_depthTexture = new GLTexture2D("PlaneClip Depth Stencil Attachment", dimension);
        m_depthTexture.Bind();
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth32fStencil8, width, height, 0, PixelFormat.DepthStencil, PixelType.Float32UnsignedInt248Rev, IntPtr.Zero);
        m_depthTexture.Unbind();

        m_framebuffer = GL.GenFramebuffer();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Framebuffer, m_framebuffer, "PlaneClip Framebuffer");
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, m_texture, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, m_depthTexture.Name, 0);
        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
            throw new Exception("Failed to complete PlaneClip framebuffer");
    }

    public unsafe void Clear()
    {
        GL.Clear(ClearBufferMask.DepthBufferBit);
        var clear = stackalloc float[2] { -1e30f, -1e30f };
        GL.ClearBuffer(ClearBuffer.Color, 0, clear);
        GL.Clear(ClearBufferMask.DepthBufferBit);
    }

    public unsafe void StartRender()
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

    public void UnbindFrameBuffer()
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
