using System;
using System.Collections.Generic;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Vertex;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Primitives;

/// <summary>
/// Renders solid lines and triangles.
/// </summary>
public class PrimitiveWorldRenderer : IDisposable
{
    private readonly List<PrimitiveVbo> m_drawItems = new();
    private readonly PrimitiveShader m_program = new();
    private bool m_disposed;
    private bool m_hasData;

    public PrimitiveWorldRenderer()
    {
        var values = (PrimitiveRenderType[])Enum.GetValues(typeof(PrimitiveRenderType));
        foreach (var value in values)
        {
            int lineWidth = 2;
            if (value == PrimitiveRenderType.Rail)
                lineWidth = 5;

            var data = new PrimitiveVbo($"Primitive {value.ToString()}", lineWidth);
            Attributes.BindAndApply(data.Vbo, data.Vao, m_program.Attributes);
            m_drawItems.Add(data);
        }        
    }

    ~PrimitiveWorldRenderer()
    {
        Dispose(false);
    }

    public void AddSegment(Seg3F segment, Vec3F color, float alpha, PrimitiveRenderType type)
    {
        PrimitiveVertex start = new(segment.Start, color, alpha);
        PrimitiveVertex end = new(segment.End, color, alpha);

        var vboData = m_drawItems[(int)type];
        vboData.Vbo.Add(start);
        vboData.Vbo.Add(end);
        m_hasData = true;
    }

    public void Render(RenderInfo renderInfo)
    {
        if (!m_hasData)
            return;

        m_program.Bind();
        m_program.Mvp(Renderer.CalculateMvpMatrix(renderInfo));
        
        for (int i = 0; i < m_drawItems.Count; i++)
        {
            var item = m_drawItems[i];
            if (item.Vbo.Empty)
                continue;

            GL.LineWidth(item.LineWidth);

            item.Vbo.UploadIfNeeded();
            item.Vao.Bind();
            item.Vbo.DrawArrays(PrimitiveType.Lines);
            item.Vao.Unbind();

            item.Vbo.Clear();
        }
        
        GL.LineWidth(1); // Any automap drawing should return to normal afterwards.

        m_program.Unbind();
        m_hasData = false;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        for (int i = 0; i < m_drawItems.Count; i++)
        {
            var item = m_drawItems[i];
            item.Vbo.Dispose();
            item.Vao.Dispose();
        }
        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
