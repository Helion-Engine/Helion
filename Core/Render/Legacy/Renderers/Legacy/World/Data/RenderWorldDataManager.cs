using System;
using System.Collections.Generic;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Texture.Legacy;
using Helion.Util;
using MoreLinq.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Renderers.Legacy.World.Data;

public class RenderWorldDataManager : IDisposable
{
    private readonly GLCapabilities m_capabilities;
    private readonly IGLFunctions gl;
    private readonly Dictionary<GLLegacyTexture, RenderWorldData> m_textureToWorldData = new Dictionary<GLLegacyTexture, RenderWorldData>();

    private readonly List<RenderWorldData> m_alphaRenderData = new();

    public RenderWorldDataManager(GLCapabilities capabilities, IGLFunctions functions)
    {
        m_capabilities = capabilities;
        gl = functions;
    }

    ~RenderWorldDataManager()
    {
        FailedToDispose(this);
        ReleaseUnmanagedResources();
    }

    public RenderWorldData GetRenderData(GLLegacyTexture texture)
    {
        if (m_textureToWorldData.TryGetValue(texture, out RenderWorldData? data))
            return data;

        RenderWorldData newData = new RenderWorldData(m_capabilities, gl, texture);
        m_textureToWorldData[texture] = newData;
        return newData;
    }

    public RenderWorldData GetAlphaRenderData(GLLegacyTexture texture)
    {
        // Since we have to order transparency drawing we can't store all the vbo data in the same texture
        // This will be a large performance penalty because we potentially have to switch textures often in the renderer
        // The RenderWorldData is at least cached new ones are not created on every render loop
        RenderWorldData data = DataCache.Instance.GetAlphaRenderWorldData(gl, m_capabilities, texture);
        m_alphaRenderData.Add(data);
        return data;
    }

    public void Clear()
    {
        m_textureToWorldData.Values.ForEach(geometryData => geometryData.Clear());
        for (int i = 0; i < m_alphaRenderData.Count; i++)
            DataCache.Instance.FreeAlphaRenderWorldData(m_alphaRenderData[i]);
        m_alphaRenderData.Clear();
    }

    public void Draw()
    {
        m_textureToWorldData.Values.ForEach(geometryData => geometryData.Draw());
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
        m_textureToWorldData.Values.ForEach(geometryData => geometryData.Dispose());
    }
}
