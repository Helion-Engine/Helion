using System;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Context;
using Helion.Resources;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Texture;

public abstract class GLTexture : IRenderableTextureHandle, IDisposable
{
    public int Index => TextureId;
    public Box2I Area => (Vec2I.Zero, Dimension.Vector);
    public Box2F UV => (Vec2F.Zero, Vec2F.One);
    public Dimension Dimension { get; }
    public Vec2I Offset { get; }
    public readonly int TextureId;
    public readonly string Name;
    public readonly Vec2F UVInverse;
    public readonly ResourceNamespace Namespace;
    public readonly TextureTarget Target;
    public readonly int TransparentPixelCount;
    private bool m_disposed;

    public int Width => Dimension.Width;
    public int Height => Dimension.Height;

    protected GLTexture(int textureId, string name, Dimension dimension, Vec2I offset, ResourceNamespace ns, TextureTarget target, int transparentPixelCount)
    {
        TextureId = textureId;
        Name = name;
        Dimension = dimension;
        Offset = offset;
        Namespace = ns;
        UVInverse = Vec2F.One / dimension.Vector.Float;
        Target = target;
        TransparentPixelCount = transparentPixelCount;
    }

    ~GLTexture()
    {
        ReleaseUnmanagedResources();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    protected virtual void ReleaseUnmanagedResources()
    {
        if (m_disposed)
            return;
        
        GL.DeleteTexture(TextureId);

        m_disposed = true;
    }
}
