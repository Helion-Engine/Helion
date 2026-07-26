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

    public RenderData(RenderProgram program, GLLegacyTexture texture, GLLegacyTexture? brightMapTexture = null) : this(program)
    {
        Set(texture, brightMapTexture);
    }

    public RenderData(RenderProgram program)
    {
        Pipeline = new(program, new DynamicVertexBuffer<TVertex>("Entity VBO"), "Entity VAO");
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
    
    public void Draw()
    {
        if (Pipeline.Empty)
            return;

        GL.ActiveTexture(BindTextures.BoundTexture);
        Texture.Bind();
        GL.ActiveTexture(BindTextures.BrightmapTexture);
        if (BrightMapTexture != null)
            BrightMapTexture.Bind();
        else
            GL.BindTexture(TextureTarget.Texture2D, 0);

        Pipeline.Bind(true);
        Pipeline.Vbo.Upload();
        Pipeline.Vbo.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4);

        Texture.Unbind();
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