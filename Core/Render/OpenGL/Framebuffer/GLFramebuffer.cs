using Helion.Geometry;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Framebuffer;

public enum GLFrameBufferOptions
{
    None,
    DepthStencilAttachment
}

public class GLFramebuffer : IDisposable
{
    public readonly string Label;
    public readonly Dimension Dimension;
    private readonly List<GLTexture2D> m_textures = [];
    private readonly int m_name;
    private bool m_disposed;

    public IReadOnlyList<GLTexture2D> Textures => m_textures;

    public GLTexture2D ColorAttachment0 = null!;
    public GLTexture2D? DepthTexture;
    public bool IsMainBackBuffer;

    public GLFramebuffer(string label, Dimension dimension, int numColorAttachments, GLFrameBufferOptions options = GLFrameBufferOptions.None, bool mainBackBuffer = false)
    {
        Debug.Assert(numColorAttachments >= 0, $"Cannot have a negative amount of color attachments for framebuffer {label}");
        Debug.Assert(dimension.HasPositiveArea, $"Must have a positive dimension for framebuffer {label}");
        Debug.Assert(numColorAttachments > 0 || options != GLFrameBufferOptions.None, "Cannot have no color attachments and no depth/stencil renderbuffer");

        Label = label;
        Dimension = dimension;
        IsMainBackBuffer = mainBackBuffer;

        if (mainBackBuffer)
            m_name = 0;
        else
            m_name = GL.GenFramebuffer();

        Bind();

        if (!mainBackBuffer)
        {
            GLHelper.ObjectLabel(ObjectLabelIdentifier.Framebuffer, m_name, $"Framebuffer: {Label}");
            CreateColorAttachments(numColorAttachments, dimension, label);
            CheckFramebufferOrThrow();
        }

        if ((options & GLFrameBufferOptions.DepthStencilAttachment) != 0)
            CreateDepthStencilAttachment(dimension, label);

        Unbind();
    }

    private void CheckFramebufferOrThrow()
    {
        FramebufferErrorCode errorCode = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (errorCode != FramebufferErrorCode.FramebufferComplete)
            throw new($"Framebuffer not complete ({Label}): {errorCode}");
    }

    private void CreateColorAttachments(int numColorAttachments, Dimension dimension, string label)
    {
        (int w, int h) = dimension;

        for (int attachmentIndex = 0; attachmentIndex < numColorAttachments; attachmentIndex++)
        {
            FramebufferAttachment attachment = FramebufferAttachment.ColorAttachment0 + attachmentIndex;

            GLTexture2D colorAttachmentTexture = new($"(Framebuffer {label}) Color Attachment {attachmentIndex}", dimension);
            colorAttachmentTexture.Bind();
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, w, h, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            GLTexture2D.SetParameters(TextureWrapMode.Clamp);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment, TextureTarget.Texture2D, colorAttachmentTexture.Name, 0);
            colorAttachmentTexture.Unbind();

            m_textures.Add(colorAttachmentTexture);
        }

        if (numColorAttachments > 0)
            ColorAttachment0 = m_textures[0];
    }

    private void CreateDepthStencilAttachment(Dimension dimension, string label)
    {
        DepthTexture = new($"(Framebuffer {label}) Depth Stencil Attachment", dimension);
        DepthTexture.Bind();
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth32fStencil8, dimension.Width, Dimension.Height, 0, PixelFormat.DepthStencil, PixelType.Float32UnsignedInt248Rev, IntPtr.Zero);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, DepthTexture.Name, 0);
        DepthTexture.Unbind();

        m_textures.Add(DepthTexture);
    }

    ~GLFramebuffer()
    {
        Dispose(false);
    }

    public void Bind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_name);
    }

    public static void Unbind() => GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    public void BindRead() => GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, m_name);
    public void BindDraw() => GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, m_name);

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        foreach (GLTexture2D texture in m_textures)
            texture.Dispose();
        m_textures.Clear();

        if (m_name != 0)
            GL.DeleteFramebuffer(m_name);

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
