using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using System;
using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderWorldDataList
{
    public List<RenderWorldData> RenderData = [];
    private RenderWorldData?[] m_allRenderData = new RenderWorldData?[1024];

    public RenderWorldData Add(GLLegacyTexture texture, RenderProgram program)
    {
        if (m_allRenderData.Length <= texture.TextureId)
        {
            var original = m_allRenderData;
            m_allRenderData = new RenderWorldData[texture.TextureId + 1024];
            Array.Copy(original, m_allRenderData, original.Length);
        }

        RenderWorldData? data = m_allRenderData[texture.TextureId];
        if (data != null)
            return data;

        RenderWorldData newData = new(texture, program);
        m_allRenderData[texture.TextureId] = newData;
        RenderData.Add(newData);
        return newData;
    }

    public void Draw()
    {
        for (int i = 0; i < RenderData.Count; i++)
            RenderData[i].Draw();
    }

    public void Clear()
    {
        for (int i = 0; i < RenderData.Count; i++)
            RenderData[i].Clear();
    }

    public void ReleaseUnmanagedResources()
    {
        for (int i = 0; i < RenderData.Count; i++)
            RenderData[i].Dispose();
    }
}
