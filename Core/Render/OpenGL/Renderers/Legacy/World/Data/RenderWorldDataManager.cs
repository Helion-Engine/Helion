using System;
using System.Collections.Generic;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util;
using Helion.Util.Container;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderWorldDataManager : IDisposable
{
    private readonly List<RenderWorldData> m_renderData = new();
    private readonly List<RenderWorldData> m_alphaRenderData = new();

    private readonly DynamicArray<RenderWorldData> m_renderedData = new();
    private readonly DynamicArray<RenderWorldData> m_renderedAlphaData = new();

    private RenderWorldData?[] m_allRenderData;
    private RenderWorldData?[] m_allRenderDataAlpha;

    private int m_renderCount = 1;

    public RenderWorldDataManager()
    {
        m_allRenderData = new RenderWorldData[1024];
        m_allRenderDataAlpha = new RenderWorldData[1024];
    }

    ~RenderWorldDataManager()
    {
        ReleaseUnmanagedResources();
    }

    public RenderWorldData GetRenderData(GLLegacyTexture texture, RenderProgram program)
    {
        if (m_allRenderData.Length <= texture.TextureId)
        {
            var original = m_allRenderData;
            m_allRenderData = new RenderWorldData[texture.TextureId + 1024];
            Array.Copy(original, m_allRenderData, original.Length);
        }

        RenderWorldData? data = m_allRenderData[texture.TextureId];
        if (data != null)
        {
            if (data.RenderCount != m_renderCount)
            {
                data.RenderCount = m_renderCount;
                m_renderedData.Add(data);
            }
            return data;
        }

        RenderWorldData newData = new(texture, program);
        m_allRenderData[texture.TextureId] = newData;       
        m_renderData.Add(newData);
        m_renderedData.Add(newData);
        return newData;
    }

    public RenderWorldData GetAlphaRenderData(GLLegacyTexture texture, RenderProgram program)
    {
        if (m_allRenderDataAlpha.Length <= texture.TextureId)
        {
            var original = m_allRenderDataAlpha;
            m_allRenderDataAlpha = new RenderWorldData[texture.TextureId + 1024];
            Array.Copy(original, m_allRenderDataAlpha, original.Length);
        }

        RenderWorldData? data = m_allRenderDataAlpha[texture.TextureId];
        if (data != null)
        {
            if (data.RenderCount != m_renderCount)
            {
                data.RenderCount = m_renderCount;
                m_renderedAlphaData.Add(data);
            }
            return data;
        }

        RenderWorldData newData = new(texture, program);
        m_allRenderDataAlpha[texture.TextureId] = newData;
        m_alphaRenderData.Add(newData);
        m_renderedAlphaData.Add(newData);
        return newData;
    }

    public void Clear()
    {
        m_renderCount++;
        for (int i = 0; i < m_renderedData.Length; i++)
            m_renderedData[i].Clear();
        for (int i = 0; i < m_renderedAlphaData.Length; i++)
            m_renderedAlphaData[i].Clear();
        m_renderedData.Clear();
        m_renderedAlphaData.Clear();
    }

    public void DrawNonAlpha()
    {
        for (int i = 0; i < m_renderedData.Length; i++)
            m_renderedData[i].Draw();
    }

    public void DrawAlpha()
    {
        for (int i = 0; i < m_renderedAlphaData.Length; i++)
            m_renderedAlphaData[i].Draw();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        for (int i = 0; i < m_renderData.Count; i++)
            m_renderData[i].Dispose();

        for (int i = 0; i < m_alphaRenderData.Count; i++)
            m_alphaRenderData[i].Dispose();
    }
}
