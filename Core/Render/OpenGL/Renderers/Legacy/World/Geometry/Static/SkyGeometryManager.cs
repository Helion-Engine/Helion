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
    public void Clear()
    {
    }

    public void AddSide(ISkyComponent sky, Side side, WallLocation wallLocation, SkyGeometryVertex[]? vertices)
    {
        if (vertices == null)
            return;

        int index = sky.Vbo.Count;
        sky.Add(vertices, vertices.Length);

        if (side.SkyGeometry == null)
            side.SkyGeometry = new();

        if (vertices == null)
            return;

        var geometryData = new StaticSkyGeometryData(sky.Vbo, index, vertices.Length);
        switch (wallLocation)
        {
            case WallLocation.Upper:
                side.SkyGeometry.Upper = geometryData;
                break;
            case WallLocation.Lower:
                side.SkyGeometry.Lower = geometryData;
                break;
            case WallLocation.Middle:
                side.SkyGeometry.Middle = geometryData;
                break;
        }
    }

    public void AddPlane(ISkyComponent sky, SectorPlane plane, SkyGeometryVertex[] vertices)
    {
        int index = sky.Vbo.Count;
        sky.Add(vertices, vertices.Length);
        plane.SkyGeometry = new(sky.Vbo, index, vertices.Length);
    }

    public void UpdateSide(Side side, WallLocation wallLocation, SkyGeometryVertex[]? vertices)
    {
        if (vertices == null)
            return;

        if (side.SkyGeometry == null)
            return;

        var data = side.SkyGeometry.Middle;
        switch (wallLocation)
        {
            case WallLocation.Upper:
                data = side.SkyGeometry.Upper;
                break;
            case WallLocation.Lower:
                data = side.SkyGeometry.Lower;
                break;
        }

        if (data != null)
            UpdateSkyGeometry(data, vertices);
    }

    public bool HasPlane(SectorPlane plane) => plane.SkyGeometry != null;
    public bool HasSide(Side side) => side.SkyGeometry != null;

    public void UpdatePlane(SectorPlane plane, SkyGeometryVertex[] vertices)
    {
        if (plane.SkyGeometry == null)
            return;

        UpdateSkyGeometry(plane.SkyGeometry, vertices);
    }

    public void ClearGeometryVertices(Side side, WallLocation wallLocation)
    {
        if (side.SkyGeometry == null)
            return;

        switch (wallLocation)
        {
            case WallLocation.Upper:
                ClearSkyGeometry(side.SkyGeometry.Upper);
                break;
            case WallLocation.Lower:
                ClearSkyGeometry(side.SkyGeometry.Lower);
                break;
            case WallLocation.Middle:
                ClearSkyGeometry(side.SkyGeometry.Middle);
                break;
        }
    }

    public void ClearGeometryVertices(SectorPlane plane)
    {
        if (plane.SkyGeometry == null)
            return;

        ClearSkyGeometry(plane.SkyGeometry);
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
