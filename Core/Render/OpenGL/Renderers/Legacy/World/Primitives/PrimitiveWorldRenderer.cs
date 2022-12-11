using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Vertex;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Primitives;

/// <summary>
/// Renders solid lines and triangles.
/// </summary>
public class PrimitiveWorldRenderer : IDisposable
{
    private readonly StreamVertexBuffer<PrimitiveVertex> m_vbo = new("Primitive");
    private readonly VertexArrayObject m_vao = new("Primitive");
    private readonly PrimitiveShader m_program = new();
    private bool m_disposed;

    public PrimitiveWorldRenderer()
    {
        Attributes.BindAndApply(m_vbo, m_vao, m_program.Attributes);
    }

    ~PrimitiveWorldRenderer()
    {
        Dispose(false);
    }

    public void AddSegment(Seg3F segment, Vec3F color, float alpha)
    {
        PrimitiveVertex start = new(segment.Start, color, alpha);
        PrimitiveVertex end = new(segment.End, color, alpha);
        m_vbo.Add(start);
        m_vbo.Add(end);
    }

    public void Render(RenderInfo renderInfo)
    {
        if (m_vbo.Empty) 
            return;

        m_program.Bind();
        m_program.Mvp(Renderer.CalculateMvpMatrix(renderInfo));

        m_vbo.UploadIfNeeded();
        m_vao.Bind();
        m_vbo.DrawArrays(PrimitiveType.Lines);
        m_vao.Unbind();

        m_program.Unbind();

        m_vbo.Clear();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        m_vbo.Dispose();
        m_vao.Dispose();
        m_program.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
