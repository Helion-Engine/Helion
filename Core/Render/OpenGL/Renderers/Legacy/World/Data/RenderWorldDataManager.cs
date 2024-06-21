using System;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderWorldDataManager : IDisposable
{
    private readonly GeometryTypeLookup<RenderWorldDataList> m_lookup = new(() =>  new RenderWorldDataList());

    ~RenderWorldDataManager()
    {
        ReleaseUnmanagedResources();
    }

    public RenderWorldData GetRenderData(GLLegacyTexture texture, RenderProgram program, GeometryType type)
    {
        var renderDataList = m_lookup.Get(type);
        return renderDataList.Add(texture, program);
    }
        
    public void Clear()
    {
        var items = m_lookup.GetItems();
        for (int i = 0; i < items.Length; i++)
            items[i].Clear();
    }

    public void RenderWalls()
    {
        m_lookup.Get(GeometryType.Wall).Draw();
    }

    public void RenderTwoSidedMiddleWalls()
    {
        m_lookup.Get(GeometryType.TwoSidedMiddleWall).Draw();
    }

    public void RenderAlphaWalls()
    {
        m_lookup.Get(GeometryType.AlphaWall).Draw();
    }

    public void RenderFlats()
    {
        m_lookup.Get(GeometryType.Flat).Draw();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        var items = m_lookup.GetItems();
        for (int i = 0; i < items.Length; i++)
            items[i].ReleaseUnmanagedResources();
    }
}
