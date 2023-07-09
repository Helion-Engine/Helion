using System;
using Helion.RenderNew.OpenGL.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.OpenGL.Textures;

public abstract class ImmutableTexture : IDisposable
{
    public readonly string Label;
    public readonly int Name;
    private readonly TextureTarget m_target;
    private bool m_disposed;

    protected ImmutableTexture(string label, TextureTarget target)
    {
        Label = label;
        Name = GL.GenTexture();
        m_target = target;

        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, Name, label);
        Unbind();
    }

    ~ImmutableTexture()
    {
        Dispose(false);
    }

    public void Bind()
    {
        GL.BindTexture(m_target, Name);
    }
    
    public void Unbind()
    {
        GL.BindTexture(m_target, 0);
    }

    public void Dispose()
    {
        if (m_disposed)
            return;
        
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();

        m_disposed = true;
    }

    private void ReleaseUnmanagedResources()
    {
        GL.DeleteTexture(Name);
    }
}