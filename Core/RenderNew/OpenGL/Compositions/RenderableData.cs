using System;
using Helion.RenderNew.OpenGL.Buffers;
using Helion.RenderNew.OpenGL.Programs;
using Helion.RenderNew.OpenGL.Vertex;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.OpenGL.Compositions;

public class RenderableData<TVertex, TRenderProgram> : IDisposable 
    where TVertex : struct 
    where TRenderProgram : RenderProgram
{
    public readonly VertexBufferObject<TVertex> Vbo;
    public readonly ElementBufferObject Ebo;
    public readonly VertexArrayObject Vao;
    public readonly TRenderProgram Program;
    private bool m_disposed;

    public RenderableData(string label, TRenderProgram program, BufferUsageHint hint, int capacity)
    {
        Vbo = new(label, hint, capacity);
        Ebo = new(label, hint, capacity);
        Vao = new(label);
        Program = program;
    }

    ~RenderableData()
    {
        ReleaseUnmanagedResources();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        ReleaseUnmanagedResources();
    }

    private void ReleaseUnmanagedResources()
    {
        if (m_disposed)
            return;
        
        Vbo.Dispose();
        Ebo.Dispose();
        Vao.Dispose();
        
        m_disposed = true;
    }
}