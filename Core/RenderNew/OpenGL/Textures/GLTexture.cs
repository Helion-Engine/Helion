using System;
using Helion.RenderNew.OpenGL.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.OpenGL.Textures;

public enum Bindless
{
    Yes,
    No
}

public abstract class GLTexture : IDisposable
{
    public readonly string Label;
    public readonly int Name;
    public long? BindlessHandle { get; protected set; }
    private readonly TextureTarget m_target;
    private bool m_disposed;

    public bool IsBindless => BindlessHandle != null; 

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
    
    public void MakeResident()
    {
        if (BindlessHandle.HasValue)
            GL.Arb.MakeTextureHandleResident(BindlessHandle.Value);
    }
    
    public void MakeNonResident()
    {
        if (BindlessHandle.HasValue)
            GL.Arb.MakeTextureHandleNonResident(BindlessHandle.Value);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;
        
        MakeNonResident();
        GL.DeleteTexture(Name);

        m_disposed = true;
    }
}