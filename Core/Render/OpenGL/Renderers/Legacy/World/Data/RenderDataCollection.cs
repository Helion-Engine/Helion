using System;
using System.Diagnostics.CodeAnalysis;
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
public class RenderDataCollection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex> : IDisposable where TVertex : struct
{
    private readonly DynamicArray<RenderData<TVertex>?> m_allRenderData = new(2048);
    private readonly DynamicArray<RenderData<TVertex>> m_dataToRender = new(2048);
    private readonly RenderProgram m_program;
    private readonly RenderDataPool<TVertex> m_renderDataPool;
    private int m_renderCount;
    private bool m_disposed;
    
    public RenderDataCollection(RenderProgram program, RenderDataPool<TVertex> renderDataPool)
    {
        m_program = program;
        m_renderDataPool = renderDataPool;
    }

    ~RenderDataCollection()
    {
        Dispose(false);
    }

    public bool HasDataToRender() => m_dataToRender.Length > 0;

    public void Clear()
    {
        for (int i = 0; i < m_dataToRender.Length; i++)
            m_dataToRender[i].Clear();
        m_dataToRender.Clear();
        
        m_renderCount++;
    }

    public DynamicArray<RenderData<TVertex>> GetDataToRender() => m_dataToRender;
    
    public RenderData<TVertex> Get(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
    {
        m_allRenderData.EnsureCapacity(texture.TextureId + 1);
        RenderData<TVertex>? data = m_allRenderData[texture.TextureId];
        
        if (data == null)
        {
            data = m_renderDataPool.Get(texture, brightmapTexture);
            data.RenderCount = m_renderCount - 1;
            m_allRenderData[texture.TextureId] = data;
        }

        if (data.RenderCount != m_renderCount)
        {
            m_dataToRender.Add(data);
            data.RenderCount = m_renderCount;
        }

        return data;
    }
    
    public void Render()
    {
        for (int i = 0; i < m_dataToRender.Length; i++)
            m_dataToRender[i].Draw();
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        for (int i = 0; i < m_allRenderData.Length; i++)
            m_allRenderData[i]?.Dispose();
        m_allRenderData.Clear();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}