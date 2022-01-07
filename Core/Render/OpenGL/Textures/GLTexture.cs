using System;
using System.Diagnostics;
using Helion.Render.OpenGL.Util;
using NLog;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures;

public class GLTexture : IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public readonly TextureTarget Target;
    public readonly string DebugName;
    public int TextureName { get; private set; }
    private bool m_disposed;

    public GLTexture(string debugName, TextureTarget target)
    {
        Target = target;
        DebugName = debugName;
        TextureName = GL.GenTexture();

        Log.Trace("Generated texture {Target} {Name} with {GLName}", target, debugName, TextureName);

        // Supposedly binding the object is sufficient to have it created, which
        // is needed to allow the binding to work.
        Bind();
        SetDebugLabel(DebugName);
        Unbind();
    }

    ~GLTexture()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    [Conditional("DEBUG")]
    public void SetDebugLabel(string textureName)
    {
        GLUtil.Label($"Texture: {textureName}", ObjectLabelIdentifier.Texture, TextureName);
    }

    public void Bind()
    {
        GL.BindTexture(Target, TextureName);
    }

    public void Unbind()
    {
        GL.BindTexture(Target, 0);
    }

    public void BindConditional(Binding binding, Action action)
    {
        if (binding == Binding.Bind)
            Bind();

        action();

        if (binding == Binding.Bind)
            Unbind();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        PerformDispose();
    }

    protected virtual void PerformDispose()
    {
        if (m_disposed)
            return;

        Log.Trace("Deleting texture {Name} ({GLName})", DebugName, TextureName);

        GL.DeleteTexture(TextureName);
        TextureName = 0;

        m_disposed = true;
    }

    public override int GetHashCode() => TextureName.GetHashCode();

    public override string ToString() => $"{DebugName} (GLName: {TextureName}, Target: {Target})";
}
