using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Vertex;
using System;

namespace Helion.Render.OpenGL.Buffer;

/// <summary>
/// A simple collection of a VBO and VAO. In the future it can be trivially
/// upgraded to having an EBO.
/// </summary>
public abstract class RenderableVertices<TVertex> : IDisposable where TVertex : struct
{
    public readonly VertexBufferObject<TVertex> Vbo;
    public readonly VertexArrayObject Vao;
    private bool m_disposed;

    protected RenderableVertices(string label, VertexBufferObject<TVertex> vbo, ProgramAttributes attributes)
    {
        Vbo = vbo;
        Vao = new(label);

        Attributes.BindAndApply(vbo, Vao, attributes);
    }

    ~RenderableVertices()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        Vbo.Dispose();
        Vao.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

public class RenderableDynamicVertices<TVertex> : RenderableVertices<TVertex> where TVertex : struct
{
    public RenderableDynamicVertices(string label, ProgramAttributes attributes) :
        base(label, new DynamicVertexBuffer<TVertex>(label), attributes)
    {
    }
}

public class RenderableStaticVertices<TVertex> : RenderableVertices<TVertex> where TVertex : struct
{
    public RenderableStaticVertices(string label, ProgramAttributes attributes) :
        base(label, new StaticVertexBuffer<TVertex>(label), attributes)
    {
    }
}

public class RenderableStreamVertices<TVertex> : RenderableVertices<TVertex> where TVertex : struct
{
    public RenderableStreamVertices(string label, ProgramAttributes attributes) :
        base(label, new StreamVertexBuffer<TVertex>(label), attributes)
    {
    }
}
