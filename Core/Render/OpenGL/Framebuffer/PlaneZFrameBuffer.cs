using Helion.Geometry;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Framebuffer;

public class PlaneZFrameBuffer : IDisposable
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

        //m_texture = GL.GenTexture();
        //GL.BindTexture(TextureTarget.Texture2D, m_texture);
        //GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, m_texture, "PlaneZ Texture");
        //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, width, height, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
        //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        //GL.BindTexture(TextureTarget.Texture2D, 0);

        //m_depthTexture = new GLTexture2D("FrameZ Depth Stencil Attachment", dimension);
        //m_depthTexture.Bind();
        //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth32fStencil8, width, height, 0, PixelFormat.DepthStencil, PixelType.Float32UnsignedInt248Rev, IntPtr.Zero);
        //m_depthTexture.Unbind();

        //m_framebuffer = GL.GenFramebuffer();
        //GLHelper.ObjectLabel(ObjectLabelIdentifier.Framebuffer, m_framebuffer, "PlaneZ Framebuffer");
        //GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_framebuffer);
        //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, m_texture, 0);
        //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, m_depthTexture.Name, 0);
        //GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

        //var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        //if (status != FramebufferErrorCode.FramebufferComplete)
        //    throw new Exception("Failed to complete planeZ framebuffer");


        GL.GenFramebuffers(1, out m_framebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_framebuffer);
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Framebuffer, (int)m_framebuffer, "PlaneZ Framebuffer");

        GL.GenTextures(1, out m_texture);
        GL.BindTexture(TextureTarget.Texture2D, m_texture);
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, (int)m_texture, "PlaneZ Texture");
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, width, height, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, m_texture, 0);

        GL.DrawBuffers(1, [DrawBuffersEnum.ColorAttachment0]);

        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            throw new Exception("Failed to complete oit framebuffer");
    }

    public unsafe void StartRender()
    {
        var error = GL.GetError();
        var min = stackalloc float[1] { -65536f };
        BindFrameBuffer();

        GL.ClearBuffer(ClearBuffer.Color, 0, min);
        GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        error = GL.GetError();
    }

    public void BindPlaneZTexture(TextureUnit textureUnit)
    {
        GL.ActiveTexture(textureUnit);
        GL.BindTexture(TextureTarget.Texture2D, m_texture);
    }

    public void BindFrameBuffer()
    {
        var error = GL.GetError();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_framebuffer);
        error = GL.GetError();
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
