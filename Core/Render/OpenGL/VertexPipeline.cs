using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Vertex;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL;

public class VertexPipeline<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex> : IDisposable
    where TVertex : struct
{
    public VertexArrayObject Vao;
    public VertexBufferObject<TVertex> Vbo;
    private bool m_disposed;

    public VertexPipeline(RenderProgram program, VertexBufferObject<TVertex> vbo, string vaoLabel)
        : this(MemoryMarshal.CreateSpan(ref program, 1), vbo, vaoLabel)
    {

    }

    public VertexPipeline(Span<RenderProgram> programs, VertexBufferObject<TVertex> vbo, string vaoLabel)
    {
        Vbo = vbo;
        Vao = new(vaoLabel, VertexArrayType.Legacy);
        Vao.Bind();
        for (int i = 0; i < programs.Length; i++)
            Attributes.BindAndApply(Vbo, Vao, programs[i].Attributes);
    }

    public void Bind(bool bindVbo = false)
    {
        Vao.Bind();
        if (bindVbo)
            Vbo.Bind();
    }

    public void Unbind()
    {
        Vbo.Unbind();
        Vao.Unbind();
    }

    public virtual void DrawArrays(PrimitiveType primitiveType = PrimitiveType.Triangles)
    {
        Vbo.DrawArrays(primitiveType);
    }

    public virtual void DrawArrays(PrimitiveType primitiveType, int first, int count)
    {
        GL.DrawArrays(PrimitiveType.Lines, first, count);
    }

    public virtual void Clear() => Vbo.Clear();

    public virtual bool Empty => Vbo.Empty;

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