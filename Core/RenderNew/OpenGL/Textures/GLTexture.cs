using System;
using Helion.RenderNew.OpenGL.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.OpenGL.Textures;

public abstract class GLTexture : IDisposable
{
    public readonly string Label;
    public readonly int Name;
    private readonly TextureTarget m_target;
    private bool m_disposed;

    protected GLTexture(string label, TextureTarget target)
    {
        Label = label;
        Name = GL.GenTexture();
        m_target = target;

        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, Name, label);
        Unbind();
    }

    ~GLTexture()
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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;
        
        GL.DeleteTexture(Name);

        m_disposed = true;
    }
}