using System.Collections.Generic;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public class StaticSkyGeometryData
{
    public VertexBufferObject<SkyGeometryVertex> Vbo;
    public int Index;
    public int Length;

    public StaticSkyGeometryData(VertexBufferObject<SkyGeometryVertex> vbo, int index, int length)
    {
        Vbo = vbo;
        Index = index;
        Length = length;
    }
}

public class StaticSideSkyData
{
    public StaticSkyGeometryData? Upper;
    public StaticSkyGeometryData? Middle;
    public StaticSkyGeometryData? Lower;
}

public sealed class SkyGeometryManager
{
    private readonly Dictionary<int, StaticSideSkyData> m_sideLookup  = new();
    private readonly Dictionary<int, StaticSkyGeometryData> m_planeLookup = new();

    public void Clear()
    {
        m_sideLookup.Clear();
        m_planeLookup.Clear();
    }

    public void AddSide(ISkyComponent sky, Side side, WallLocation wallLocation, SkyGeometryVertex[]? vertices)
    {
        if (vertices == null)
            return;

        int index = sky.Vbo.Count;
        sky.Add(vertices, vertices.Length);

        if (!m_sideLookup.TryGetValue(side.Id, out var data))
        {
            data = new();
            m_sideLookup[side.Id] = data;
        }

        if (vertices == null)
            return;

        var geometryData = new StaticSkyGeometryData(sky.Vbo, index, vertices.Length);
        switch (wallLocation)
        {
            case WallLocation.Upper:
                data.Upper = geometryData;
                break;
            case WallLocation.Lower:
                data.Lower = geometryData;
                break;
            case WallLocation.Middle:
                data.Middle = geometryData;
                break;
        }
    }

    public void AddPlane(ISkyComponent sky, SectorPlane plane, SkyGeometryVertex[] vertices)
    {
        int index = sky.Vbo.Count;
        sky.Add(vertices, vertices.Length);
        m_planeLookup[plane.Id] = new StaticSkyGeometryData(sky.Vbo, index, vertices.Length);
    }

    public void UpdateSide(Side side, WallLocation wallLocation, SkyGeometryVertex[]? vertices)
    {
        if (vertices == null)
            return;

        if (!m_sideLookup.TryGetValue(side.Id, out var sideData))
            return;

        var data = sideData.Middle;
        switch (wallLocation)
        {
            case WallLocation.Upper:
                data = sideData.Upper;
                break;
            case WallLocation.Lower:
                data = sideData.Lower;
                break;
        }

        if (data != null)
            UpdateSkyGeometry(data, vertices);
    }

    public bool HasPlane(SectorPlane plane) => m_planeLookup.ContainsKey(plane.Id);
    public bool HasSide(Side side) => m_sideLookup.ContainsKey(side.Id);

    public void UpdatePlane(SectorPlane plane, SkyGeometryVertex[] vertices)
    {
        if (!m_planeLookup.TryGetValue(plane.Id, out var data))
            return;

        UpdateSkyGeometry(data, vertices);
    }

    public void ClearGeometryVertices(Side side, WallLocation wallLocation)
    {
        if (!m_sideLookup.TryGetValue(side.Id, out var sideData))
            return;

        switch (wallLocation)
        {
            case WallLocation.Upper:
                ClearSkyGeometry(sideData.Upper);
                break;
            case WallLocation.Lower:
                ClearSkyGeometry(sideData.Lower);
                break;
            case WallLocation.Middle:
                ClearSkyGeometry(sideData.Middle);
                break;
        }
    }

    public void ClearGeometryVertices(SectorPlane plane)
    {
        if (!m_planeLookup.TryGetValue(plane.Id, out var data))
            return;

        ClearSkyGeometry(data);
    }

    private static void ClearSkyGeometry(StaticSkyGeometryData? data)
    {
        if (data == null)
            return;

        for (int i = 0; i < data.Length; i++)
        {
            int index = data.Index + i;
            data.Vbo.Data.Data[index].X = 0;
            data.Vbo.Data.Data[index].Y = 0;
            data.Vbo.Data.Data[index].Z = 0;
        }

        data.Vbo.Bind();
        data.Vbo.UploadSubData(data.Index, data.Length);
    }

    private static void UpdateSkyGeometry(StaticSkyGeometryData data, SkyGeometryVertex[] vertices)
    {
        for (int i = 0; i < data.Length; i++)
        {
            int index = data.Index + i;
            data.Vbo.Data.Data[index] = vertices[i];
        }

        data.Vbo.Bind();
        data.Vbo.UploadSubData(data.Index, data.Length);
    }
}
