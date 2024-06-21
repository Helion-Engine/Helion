using System;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderWorldDataManager : IDisposable
{
    private readonly RenderWorldDataList m_renderDataWall = new();
    private readonly RenderWorldDataList m_alphaRenderDataWall = new();
    private readonly RenderWorldDataList m_renderDataFlat = new();

    ~RenderWorldDataManager()
    {
        ReleaseUnmanagedResources();
    }

    private RenderWorldDataList GetRenderDataList(GeometryType type, bool alpha)
    {
        if (type == GeometryType.Flat)
            return m_renderDataFlat;

        if (alpha)
            return m_alphaRenderDataWall;
        return m_renderDataWall;
    }

    public RenderWorldData GetRenderData(GLLegacyTexture texture, RenderProgram program, GeometryType type, bool alpha)
    {
        var renderDataList = GetRenderDataList(type, alpha);
        return renderDataList.Add(texture, program);
    }

    
    public void Clear()
    {
        m_renderDataWall.Clear();
        m_renderDataFlat.Clear();
    }

    public void RenderWalls()
    {
        m_renderDataWall.Draw();
    }

    public void RenderAlphaWalls()
    {
        m_alphaRenderDataWall.Draw();
    }

    public void RenderFlats()
    {
        m_renderDataFlat.Draw();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        m_renderDataWall.ReleaseUnmanagedResources();
        m_renderDataFlat.ReleaseUnmanagedResources();
    }
}
