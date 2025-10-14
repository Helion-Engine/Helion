using Helion.Geometry;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Framebuffer;

public class OitFrameBuffer
{
    private uint m_oitFramebuffer;
    private uint m_accumTexture;
    private uint m_accumCountTexture;
    private uint m_fuzzTexture;
    private Dimension m_oitDimension;

    public void CreateOrUpdate(Dimension dimension, GLTexture2D opaqueDepthTexture)
    {
        if (m_oitFramebuffer != 0 && dimension.Width == m_oitDimension.Width && dimension.Height == m_oitDimension.Height)
            return;

        if (m_oitFramebuffer != 0)
        {
            GL.DeleteTexture(m_accumTexture);
            GL.DeleteTexture(m_accumCountTexture);
            GL.DeleteFramebuffer(m_oitFramebuffer);
        }

        m_oitDimension = dimension;
        var width = m_oitDimension.Width;
        var height = m_oitDimension.Height;
        GL.GenFramebuffers(1, out m_oitFramebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_oitFramebuffer);
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Framebuffer, (int)m_oitFramebuffer, "OIT Framebuffer");

        GL.GenTextures(1, out m_accumTexture);
        GL.BindTexture(TextureTarget.Texture2D, m_accumTexture);
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, (int)m_accumTexture, "OIT Accum Texture");
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, width, height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        GL.GenTextures(1, out m_accumCountTexture);
        GL.BindTexture(TextureTarget.Texture2D, m_accumCountTexture);
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, (int)m_accumCountTexture, "OIT Accum Count Texture");
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rg16f, width, height, 0, PixelFormat.Rg, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        GL.GenTextures(1, out m_fuzzTexture);
        GL.BindTexture(TextureTarget.Texture2D, m_fuzzTexture);
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, (int)m_fuzzTexture, "OIT Fuzz Texture");
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, width, height, 0, PixelFormat.Red, PixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, m_accumTexture, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, m_accumCountTexture, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D, m_fuzzTexture, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, opaqueDepthTexture.Name, 0);

        GL.DrawBuffers(3, [DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2]);

        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            throw new Exception("Failed to complete oit framebuffer");
    }

    public unsafe void StartRender()
    {
        var zero = stackalloc float[4] { 0f, 0f, 0f, 0f };
        BindFrameBuffer();

        GL.ClearBuffer(ClearBuffer.Color, 0, zero);
        GL.ClearBuffer(ClearBuffer.Color, 1, zero);
        GL.ClearBuffer(ClearBuffer.Color, 2, zero);

        SetBlendEquations();
    }

    public static void SetBlendEquations()
    {
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
    }

    public void BindFrameBuffer()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_oitFramebuffer);
    }

    public static void UnbindFrameBuffer()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void BindTextures(TextureUnit accumTexture, TextureUnit accumCountTexture, TextureUnit fuzzTexture, TextureUnit opaqueTexture, GLFramebuffer framebuffer)
    {
        GL.ActiveTexture(accumTexture);
        GL.BindTexture(TextureTarget.Texture2D, m_accumTexture);
        GL.ActiveTexture(accumCountTexture);
        GL.BindTexture(TextureTarget.Texture2D, m_accumCountTexture);
        GL.ActiveTexture(fuzzTexture);
        GL.BindTexture(TextureTarget.Texture2D, m_fuzzTexture);
        GL.ActiveTexture(opaqueTexture);
        GL.BindTexture(TextureTarget.Texture2D, framebuffer.ColorAttachment0.Name);
    }
}
