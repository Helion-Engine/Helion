using System;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util.Assertion;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public sealed class RenderWorldDataManager : StyleRendererBase, IDisposable
{
    private readonly GeometryTypeLookup<RenderWorldDataList> m_lookup = new(() => new RenderWorldDataList());
    private RenderWorldData? m_coverWalls;

    public bool BufferCoverWalls = true;

    ~RenderWorldDataManager()
    {
        ReleaseUnmanagedResources();
    }

    public void InitCoverWallRenderData(GLLegacyTexture texture, RenderProgram program, GLLegacyTexture? brightmapTexture = null)
    {
        m_coverWalls ??= new(texture, program, brightmapTexture);
    }

    public RenderWorldData GetRenderData(GLLegacyTexture texture, RenderProgram program, GeometryType type, GLLegacyTexture? brightmapTexture = null)
    {
        var renderDataList = m_lookup.Get(type);
        return renderDataList.Add(texture, program, brightmapTexture);
    }

    public void AddCoverWallVertices(Side side, Span<DynamicVertex> vertices, WallLocation location, bool oneSided)
    {
        if (m_coverWalls == null || !BufferCoverWalls)
            return;

        Assert.Precondition(vertices.Length == 6, "Wall vertices should be 6");

        int index = m_coverWalls.Pipeline.Vbo.Data.Length;
        m_coverWalls.Pipeline.Vbo.Add(vertices);
        CoverWallUtil.SetCoverWallVertices(side, m_coverWalls.Pipeline.Vbo.Data.Data, index, location);
    }

    public void AddCoverFlatVertices(DynamicVertex[] vertices)
    {
        if (m_coverWalls == null || !BufferCoverWalls)
            return;

        m_coverWalls.Pipeline.Vbo.Add(vertices);
    }

    public void Clear()
    {
        var items = m_lookup.GetItems();
        for (int i = 0; i < items.Length; i++)
            items[i].Clear();

        m_coverWalls?.Clear();
    }

    public void RenderWalls()
    {
        m_lookup.Get(GeometryType.Wall).Draw();
    }

    public void RenderTwoSidedMiddleWalls()
    {
        m_lookup.Get(GeometryType.TwoSidedMiddleWall).Draw();
    }

    public void RenderMiddle3D()
    {
        m_lookup.Get(GeometryType.Middle3D).Draw();
    }

    public void RenderFlats()
    {
        m_lookup.Get(GeometryType.Flat).Draw();
    }

    public void RenderCoverWalls()
    {
        m_coverWalls?.Draw();
    }

    public void Render(GeometryType type)
    {
        m_lookup.Get(type).Draw();
    }

    public override void Render(RenderDataStyle style)
    {
        m_lookup.Get(style.ToGeometryType()).Draw();
    }

    public override bool HasStyleToRender(RenderDataStyle style)
    {
        return m_lookup.Get(style.ToGeometryType()).HasDataToRender();
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
        m_coverWalls?.Dispose();
    }
}
