using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderData<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex> : IDisposable where TVertex : struct
{
    public DynamicVertexBuffer<TVertex> Vbo;
    public VertexArrayObject Vao;
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
        Vao = new("Entity VAO");
        Vbo = new("Entity VBO");
        Attributes.BindAndApply(Vbo, Vao, program.Attributes);
        ArrayData = Vbo.Data;
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
        Vbo.Clear();
    }
    
    public void Draw()
    {
        if (Vbo.Empty)
            return;

        GL.ActiveTexture(BindTextures.BoundTexture);
        Texture.Bind();
        GL.ActiveTexture(BindTextures.BrightmapTexture);
        if (BrightMapTexture != null)
            BrightMapTexture.Bind();
        else
            GL.BindTexture(TextureTarget.Texture2D, 0);
        Vao.Bind();
        Vbo.Bind();

        Vbo.Upload();
        Vbo.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4);

        Vbo.Unbind();
        Vao.Unbind();
        Texture.Unbind();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        Vao.Dispose();
        Vbo.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}