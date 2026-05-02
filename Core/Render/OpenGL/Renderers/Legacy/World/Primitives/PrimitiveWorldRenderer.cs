using System;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Vertex;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Primitives;

/// <summary>
/// Renders solid lines and triangles.
/// </summary>
public class PrimitiveWorldRenderer : IDisposable
{
    private readonly DynamicArray<PrimitiveVbo> m_drawItems = [];
    private readonly DynamicArray<PrimitiveVbo> m_drawItemsTransparent = [];
    private readonly PrimitiveShader m_program = new();
    private bool m_disposed;

    public bool HasOpaque;
    public bool HasTransparent;

    public PrimitiveWorldRenderer()
    {
        var values = Enum.GetValues<PrimitiveRenderType>();
        SetupVbo(values, m_drawItems);
        SetupVbo(values, m_drawItemsTransparent);
    }

    private void SetupVbo(PrimitiveRenderType[] values, DynamicArray<PrimitiveVbo> items)
    {
        foreach (var value in values)
        {
            int lineWidth = 2;
            if (value == PrimitiveRenderType.Rail)
                lineWidth = 5;

            var data = new PrimitiveVbo($"Primitive {value}", lineWidth);
            Attributes.BindAndApply(data.Vbo, data.Vao, m_program.Attributes);
            items.Add(data);
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

        bool transparent = alpha < 1;

        var vboData = transparent ? m_drawItemsTransparent[(int)type] : m_drawItems[(int)type];
        vboData.Vbo.Add(start);
        vboData.Vbo.Add(end);
        if (transparent)
            HasTransparent |= true;
        else
            HasOpaque |= true;
    }

    public void RenderAll(RenderInfo renderInfo)
    {
        RenderOpaque(renderInfo);
        RenderTransparent(renderInfo);
    }

    public void RenderOpaque(RenderInfo renderInfo)
    {
        if (HasOpaque)
            Render(renderInfo, m_drawItems);
    }

    public void RenderTransparent(RenderInfo renderInfo)
    {
        if (HasTransparent)
            Render(renderInfo, m_drawItemsTransparent);
    }

    public void Clear()
    {
        if (HasOpaque)
            Clear(m_drawItems);
        if (HasTransparent)
            Clear(m_drawItemsTransparent);
        HasOpaque = false;
        HasTransparent = false;
    }

    private static void Clear(DynamicArray<PrimitiveVbo> items)
    {
        for (int i = 0; i < items.Length; i++)
            items[i].Vbo.Clear();
    }

    private void Render(RenderInfo renderInfo, DynamicArray<PrimitiveVbo> items)
    {
        m_program.Bind();
        m_program.Mvp(renderInfo.Uniforms.Mvp);

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item.Vbo.Empty)
                continue;

            GL.LineWidth(item.LineWidth);

            item.Vbo.UploadIfNeeded();
            item.Vao.Bind();
            item.Vbo.DrawArrays(PrimitiveType.Lines);
            item.Vao.Unbind();
        }

        GL.LineWidth(1); // Any automap drawing should return to normal afterwards.

        m_program.Unbind();
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
