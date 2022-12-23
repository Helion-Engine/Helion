using System;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

/// <summary>
/// A collection of render data for specific textures. This exists because we want
/// to track vertices for alpha and non-alpha, but keep them in separate lists.
/// Instead of copy pasting the logic, they're now in their own class.
/// </summary>
public class RenderDataCollection<TVertex> : IDisposable where TVertex : struct
{
    private readonly DynamicArray<RenderData<TVertex>?> m_allRenderData = new();
    private readonly DynamicArray<RenderData<TVertex>> m_dataToRender = new();
    private readonly RenderProgram m_program;
    private int m_renderCount;
    private bool m_disposed;
    
    public RenderDataCollection(RenderProgram program)
    {
        m_program = program;
    }

    ~RenderDataCollection()
    {
        Dispose(false);
    }
    
    public void Clear()
    {
        for (int i = 0; i < m_dataToRender.Length; i++)
            m_dataToRender[i].Clear();
        m_dataToRender.Clear();
        
        m_renderCount++;
    }
    
    public RenderData<TVertex> Get(GLLegacyTexture texture)
    {
        if (texture.TextureId >= m_allRenderData.Length)
            ResizeToSupportIndex(texture.TextureId);

        RenderData<TVertex>? data = m_allRenderData[texture.TextureId];
        
        if (data == null)
        {
            data = new(texture, m_program) { RenderCount = m_renderCount - 1 };
            m_allRenderData[texture.TextureId] = data;
        }

        if (data.RenderCount != m_renderCount)
        {
            m_dataToRender.Add(data);
            data.RenderCount = m_renderCount;
        }

        return data;
    }
    
    private void ResizeToSupportIndex(int index)
    {
        const int GrowthSize = 1024;

        int nextLargestSize = ((index / GrowthSize) + 1) * GrowthSize;
        m_allRenderData.Resize(nextLargestSize);
    }
    
    public void Render(PrimitiveType primitive)
    {
        for (int i = 0; i < m_dataToRender.Length; i++)
            m_dataToRender[i].Draw(primitive);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        for (int i = 0; i < m_allRenderData.Length; i++)
            m_allRenderData[i].Dispose();
        m_allRenderData.Clear();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}