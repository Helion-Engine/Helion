using System;
using Helion.Render.OpenGLNew.Capabilities;
using Helion.Render.OpenGLNew.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Textures;

public abstract class Texture : IBindable
{
    public readonly string Label;
    private int m_objectId;
    private bool m_disposed;
    
    protected abstract TextureTarget Target { get; }

    public Texture(string label)
    {
        Label = label;
        m_objectId = GL.GenTexture();

        Bind();
        GLUtil.ObjectLabel(ObjectLabelIdentifier.Texture, m_objectId, $"[Texture] {label}");
        Unbind();
    }
    
    ~Texture()
    {
        ReleaseUnmanagedResources();
    }

    public abstract void GenerateMipmaps();
    
    public void SetFilter(TextureMinFilter min, TextureMagFilter mag)
    {
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)min);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)mag);
    }
    
    public void SetWrap(TextureWrapMode wrap)
    {
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrap);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrap);
    }
    
    public void SetAnisotropy(float anisotropy)
    {
        if (!GLExtensions.TextureFilterAnisotropic || anisotropy < 1)
            return;

        float value = Math.Clamp(anisotropy, 1, (int)GLLimits.MaxAnisotropy);
        GL.TexParameter(Target, (TextureParameterName)All.TextureMaxAnisotropy, value);
    }
    
    public void Bind()
    {
        GL.BindTexture(Target, m_objectId);
    }

    public void Unbind()
    {
        GL.BindTexture(Target, GLUtil.NoObject);
    }

    private void ReleaseUnmanagedResources()
    {
        if (m_disposed)
            return;

        GL.DeleteTexture(m_objectId);
        m_objectId = GLUtil.NoObject;

        m_disposed = true;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}