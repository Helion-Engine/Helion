using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderData<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex> : IDisposable where TVertex : struct
{
    public VertexPipeline<TVertex> Pipeline;
    public GLLegacyTexture Texture;
    public GLLegacyTexture? BrightMapTexture;
    public DynamicArray<TVertex> ArrayData;
    public int RenderCount;
    private bool m_disposed;

    public RenderData(RenderProgram program, int capacity, GLLegacyTexture texture, GLLegacyTexture? brightMapTexture = null) : this(program, capacity)
    {
        Set(texture, brightMapTexture);
    }

    public RenderData(RenderProgram program, int capacity)
    {
        Pipeline = new(program, new DynamicVertexBuffer<TVertex>("Entity VBO", capacity), "Entity VAO");
        ArrayData = Pipeline.Vbo.Data;
        Texture = null!;
    }
    
    public void Set(GLLegacyTexture texture, GLLegacyTexture? brightMapTexture = null)
    {
        Texture = texture;
        BrightMapTexture = brightMapTexture;
    }

    ~RenderData()
    {
        Dispose(false);
    }
    
    public void Clear()
    {
        Pipeline.Clear();
    }
    
    public bool Draw()
    {
        if (Pipeline.Empty)
            return false;

        GL.ActiveTexture(BindTextures.BoundTexture);
        Texture.Bind();
        GL.ActiveTexture(BindTextures.BrightmapTexture);
        if (BrightMapTexture != null)
            BrightMapTexture.Bind();
        else
            GL.BindTexture(Texture.Target, 0);

        Pipeline.Bind(true);
        Pipeline.Vbo.Upload();
        Pipeline.Vbo.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4);

        Texture.Unbind();
        return true;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        Pipeline.Dispose();
        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}