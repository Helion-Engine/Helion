using System;
using System.Collections.Generic;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderWorldDataManager : IDisposable
{
    private readonly List<RenderWorldData> m_renderData = new();
    private readonly List<RenderWorldData> m_alphaRenderData = new();

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
                data.RenderCount = m_renderCount;
            return data;
        }

        RenderWorldData newData = new(texture, program);
        m_allRenderData[texture.TextureId] = newData;       
        m_renderData.Add(newData);
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
                data.RenderCount = m_renderCount;
            return data;
        }

        RenderWorldData newData = new(texture, program);
        m_allRenderDataAlpha[texture.TextureId] = newData;
        m_alphaRenderData.Add(newData);
        return newData;
    }

    public void Clear()
    {
        m_renderCount++;
        for (int i = 0; i < m_renderData.Count; i++)
            m_renderData[i].Clear();

        for (int i = 0; i < m_alphaRenderData.Count; i++)
            m_alphaRenderData[i].Clear();
    }

    public void DrawNonAlpha()
    {
        for (int i = 0; i < m_renderData.Count; i++)
            m_renderData[i].Draw();
    }

    public void DrawAlpha()
    {
        for (int i = 0; i < m_alphaRenderData.Count; i++)
            m_alphaRenderData[i].Draw();
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
