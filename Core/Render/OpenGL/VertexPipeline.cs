using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Context;
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
    private static VertexArrayObject? SharedVao;

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

        if (GLInfo.DsaSupported)
        {
            if (SharedVao == null)
            {
                SharedVao = new(vaoLabel, VertexArrayType.Modern);
                for (int i = 0; i < programs.Length; i++)
                    Attributes.ApplyModern(Vbo, SharedVao, programs[i].Attributes);
            }

            Vao = SharedVao;
        }
        else
        {
            Vao = new(vaoLabel, VertexArrayType.Legacy);
            Vao.Bind();
            for (int i = 0; i < programs.Length; i++)
                Attributes.BindAndApply(Vbo, Vao, programs[i].Attributes);
        }
    }

    public void Bind(bool bindVbo = false)
    {
        if (GLInfo.DsaSupported)
        {
            Vao.Bind();
            Attributes.BindVertexArrayBuffer(Vao, Vbo);
            if (bindVbo)
                Vbo.Bind();
        }
        else
        {
            Vao.Bind();
            Vbo.Bind();
        }
    }

    public void Unbind()
    {
        if (!GLInfo.DsaSupported)
        {
            Vbo.Unbind();
            Vao.Unbind();
        }
    }

    public virtual void DrawArrays(PrimitiveType primitiveType = PrimitiveType.Triangles)
    {
        Vbo.DrawArrays(primitiveType);
    }

    public virtual void Clear() => Vbo.Clear();

    public virtual bool Empty => Vbo.Empty;

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        if (!GLInfo.DsaSupported)
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