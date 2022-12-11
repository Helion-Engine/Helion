using Helion.Geometry;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Framebuffer;

public class GLFramebuffer : IDisposable
{
    public readonly string Label;
    public readonly Dimension Dimension;
    private readonly List<GLTexture2D> m_textures = new();
    private readonly GLRenderbuffer? m_renderBuffer;
    private readonly int m_name;
    private bool m_disposed;

    public IReadOnlyList<GLTexture2D> Textures => m_textures;

    public GLFramebuffer(string label, Dimension dimension, int numColorAttachments, RenderbufferStorage? storage = null)
    {
        Debug.Assert(numColorAttachments >= 0, $"Cannot have a negative amount of color attachments for framebuffer {label}");
        Debug.Assert(dimension.HasPositiveArea, $"Must have a positive dimension for framebuffer {label}");
        Debug.Assert(numColorAttachments > 0 || storage != null, "Cannot have no color attachments and no depth/stencil renderbuffer");

        Label = label;
        Dimension = dimension;
        m_name = GL.GenFramebuffer();
        if (storage != null)
            m_renderBuffer = new(label, dimension, storage.Value);

        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Framebuffer, m_name, $"Framebuffer: {Label}");
        CreateColorAttachments(numColorAttachments, dimension, label);
        if (storage != null)
            AttachRenderBuffer();
        CheckFramebufferOrThrow();
        Unbind();
    }

    private void CheckFramebufferOrThrow()
    {
        FramebufferErrorCode errorCode = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (errorCode != FramebufferErrorCode.FramebufferComplete)
            throw new($"Framebuffer not complete ({Label}): {errorCode}");
    }

    private void AttachRenderBuffer()
    {
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, m_renderBuffer.Name);
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
            colorAttachmentTexture.SetParameters(TextureWrapMode.Clamp);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment, TextureTarget.Texture2D, colorAttachmentTexture.Name, 0);
            colorAttachmentTexture.Unbind();

            m_textures.Add(colorAttachmentTexture);
        }
    }

    ~GLFramebuffer()
    {
        Dispose(false);
    }

    public void Bind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_name);
    }

    public void Unbind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        foreach (GLTexture2D texture in m_textures)
            texture.Dispose();
        m_textures.Clear();
        m_renderBuffer?.Dispose();
        GL.DeleteFramebuffer(m_name);

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
