using System;
using System.Diagnostics.CodeAnalysis;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util.Container;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public enum RenderDataCollectionMode
{
    // The RenderData is pinned to the texture. Get will always return the same RenderData for requested texture.
    Pinned,
    // The RenderData is recycled when cleared. Any Get will return the next available RenderData. This keeps the list smaller for varying textures.
    Recycle
}

/// <summary>
/// A collection of render data for specific textures. This exists because we want
/// to track vertices for alpha and non-alpha, but keep them in separate lists.
/// Instead of copy pasting the logic, they're now in their own class.
/// </summary>
public class RenderDataCollection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex> : IDisposable where TVertex : struct
{
    private readonly LookupArray<RenderData<TVertex>?> m_allRenderData = new(2048);
    private readonly DynamicArray<RenderData<TVertex>> m_dataToRender = new(2048);
    private readonly RenderDataPool<TVertex> m_renderDataPool;
    private readonly RenderDataCollectionMode m_mode;
    private readonly Action? m_onDraw;
    private int m_renderCount;
    private bool m_disposed;
    
    public RenderDataCollection(RenderDataPool<TVertex> renderDataPool, RenderDataCollectionMode mode, Action? onDraw = null)
    {
        m_renderDataPool = renderDataPool;
        m_mode = mode;
        m_onDraw = onDraw;
    }

    ~RenderDataCollection()
    {
        Dispose(false);
    }

    public bool HasDataToRender() => m_dataToRender.Length > 0;

    public void Clear()
    {
        for (int i = 0; i < m_dataToRender.Length; i++)
        {
            var data = m_dataToRender.Data[i];
            data.Clear();
            if (m_mode == RenderDataCollectionMode.Recycle)
            {
                m_allRenderData.Set(data.Texture.TextureId, null);
                m_renderDataPool.Return(data);
            }
        }

        m_dataToRender.Clear();
        m_renderCount++;
    }

    public DynamicArray<RenderData<TVertex>> GetDataToRender() => m_dataToRender;
    
    public RenderData<TVertex> Get(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
    {
        if (m_mode == RenderDataCollectionMode.Pinned)
            return GetStatic(texture, brightmapTexture);

        return GetDynamic(texture, brightmapTexture);
    }

    private RenderData<TVertex> GetDynamic(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture)
    {
        if (!m_allRenderData.TryGetValue(texture.TextureId, out var data))
        {
            data = m_renderDataPool.Get(texture, brightmapTexture);
            m_allRenderData.Set(texture.TextureId, data);
        }

        if (data.RenderCount != m_renderCount)
        {
            m_dataToRender.Add(data);
            data.RenderCount = m_renderCount;
        }

        return data;
    }

    private RenderData<TVertex> GetStatic(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture)
    {
        if (!m_allRenderData.TryGetValue(texture.TextureId, out var data))
        {
            data = m_renderDataPool.Get(texture, brightmapTexture);
            data.RenderCount = m_renderCount - 1;
            m_allRenderData.Set(texture.TextureId, data);
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
        if (m_dataToRender.Length == 0)
            return;

        for (int i = 0; i < m_dataToRender.Length; i++)
        {
            if (m_dataToRender[i].Draw())
                m_onDraw?.Invoke();
        }
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        var renderDataItems = m_allRenderData.GetData();
        for (int i = 0; i < renderDataItems.Length; i++)
            renderDataItems[i]?.Dispose();
        m_allRenderData.SetAll(null);

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}