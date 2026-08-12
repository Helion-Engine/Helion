using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util.Container;
using System;
using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderWorldDataList(RenderWorldDataPool pool)
{
    public List<RenderWorldData> RenderData = [];
    private RenderWorldData?[] m_allRenderData = new RenderWorldData?[1024];
    private readonly DynamicArray<RenderWorldData> m_dataToRender = new(1024);
    private readonly RenderWorldDataPool m_pool = pool;
    private int m_renderCount;

    public void Reset()
    {
        for (int i = 0; i < m_allRenderData.Length; i++)
        {
            var renderData = m_allRenderData[i];
            if (renderData != null)
                m_pool.Add(renderData);

            m_allRenderData[i] = null;
        }

        RenderData.Clear();
    }

    public RenderWorldData Add(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
    {
        if (m_allRenderData.Length <= texture.TextureId)
        {
            var original = m_allRenderData;
            m_allRenderData = new RenderWorldData[texture.TextureId + 1024];
            Array.Copy(original, m_allRenderData, original.Length);
        }

        var data = m_allRenderData[texture.TextureId];
        if (data == null)
        {
            data = m_pool.Get(texture, brightmapTexture);
            m_allRenderData[texture.TextureId] = data;
            RenderData.Add(data);
        }

        if (data.RenderCount != m_renderCount)
        {
            m_dataToRender.Add(data);
            data.RenderCount = m_renderCount;
        }

        return data;
    }

    public bool HasDataToRender() => m_dataToRender.Length > 0;

    public void Draw()
    {
        for (int i = 0; i < m_dataToRender.Length; i++)
            m_dataToRender[i].Draw();
    }

    public void Clear()
    {
        for (int i = 0; i < m_dataToRender.Length; i++)
            m_dataToRender[i].Clear();
        m_dataToRender.Clear();

        m_renderCount++;
    }

    public void ReleaseUnmanagedResources()
    {
        for (int i = 0; i < RenderData.Count; i++)
            RenderData[i].Dispose();
    }
}
