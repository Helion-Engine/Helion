using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Geometry.Sectors;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

public class FloodFillRenderer : IDisposable
{
    const int VerticesPerWall = 6;

    private readonly LegacyGLTextureManager m_textureManager;
    private readonly FloodFillProgram m_program = new();
    private readonly List<FloodFillInfo> m_floodFillInfos = new();
    private readonly Dictionary<int, int> m_textureHandleToFloodFillInfoIndex = new();
    private readonly DynamicArray<FloodGeometry> m_floodGeometry = new();
    private readonly List<FloodGeometry> m_freeData = new();
    private bool m_disposed;

    public FloodFillRenderer(LegacyGLTextureManager textureManager)
    {
        m_textureManager = textureManager;
    }

    ~FloodFillRenderer()
    {
        Dispose(false);
    }

    public void UpdateTo(IWorld world)
    {
        DisposeAndClearData();
    }

    private FloodFillInfo CreateFloodFillInfo(SectorPlane plane)
    {
        string label = $"Flood fill (Texture {plane.TextureHandle}, Z = {plane.Z})";
        RenderableStaticVertices<FloodFillVertex> vertices = new(label, m_program.Attributes);
        return new(plane.TextureHandle, plane.Z, vertices);
    }

    private unsafe FloodFillInfo GetOrCreateFloodFillInfo(SectorPlane plane)
    {
        if (m_textureHandleToFloodFillInfoIndex.TryGetValue(plane.TextureHandle, out int index))
            return m_floodFillInfos[index];

        FloodFillInfo floodInfo = CreateFloodFillInfo(plane);
        m_textureHandleToFloodFillInfoIndex[plane.TextureHandle] = m_floodFillInfos.Count;
        m_floodFillInfos.Add(floodInfo);
        return floodInfo;
    }

    private bool TryGetFloodGeometry(int floodKey, out FloodGeometry geometry)
    {
        floodKey--;
        if (floodKey < 0 || floodKey >= m_floodGeometry.Length)
        {
            geometry = default;
            return false;
        }
        geometry = m_floodGeometry[floodKey];
        return true;
    }

    public void UpdateStaticWall(int floodKey, SectorPlane floodPlane, WallVertices vertices, double minPlaneZ, double maxPlaneZ)
    {
        if (!TryGetFloodGeometry(floodKey, out var data))
            return;

        if (!m_textureHandleToFloodFillInfoIndex.TryGetValue(data.TextureHandle, out int index))
            return;

        FloodFillInfo floodInfo = m_floodFillInfos[index];
        float minZ = (float)minPlaneZ;
        float maxZ = (float)maxPlaneZ;
        float planeZ = (float)floodPlane.Z;
        float prevPlaneZ = (float)floodPlane.PrevZ;

        float topZ = vertices.TopLeft.Z;
        float bottomZ = vertices.BottomLeft.Z;
        float prevTopZ = vertices.PrevTopZ;
        float prevBottomZ = vertices.PrevBottomZ;

        FloodFillVertex topLeft = new((vertices.TopLeft.X, vertices.TopLeft.Y, topZ),
            prevTopZ, planeZ, prevPlaneZ, minZ, maxZ, data.LightIndex);
        FloodFillVertex topRight = new((vertices.TopRight.X, vertices.TopRight.Y, topZ),
            prevTopZ, planeZ, prevPlaneZ, minZ, maxZ, data.LightIndex);
        FloodFillVertex bottomLeft = new((vertices.BottomLeft.X, vertices.BottomLeft.Y, bottomZ),
            prevBottomZ, planeZ, prevPlaneZ, minZ, maxZ, data.LightIndex);
        FloodFillVertex bottomRight = new((vertices.BottomRight.X, vertices.BottomRight.Y, bottomZ),
            prevBottomZ, planeZ, prevPlaneZ, minZ, maxZ, data.LightIndex);

        var vbo = floodInfo.Vertices.Vbo;
        vbo.Data[data.VboOffset] = topLeft;
        vbo.Data[data.VboOffset + 1] = bottomLeft;
        vbo.Data[data.VboOffset + 2] = topRight;
        vbo.Data[data.VboOffset + 3] = topRight;
        vbo.Data[data.VboOffset + 4] = bottomLeft;
        vbo.Data[data.VboOffset + 5] = bottomRight;

        vbo.Bind();
        vbo.UploadSubData(data.VboOffset, VerticesPerWall);
    }

    public int AddStaticWall(SectorPlane sectorPlane, WallVertices vertices, double minPlaneZ, double maxPlaneZ)
    {
        float minZ = (float)minPlaneZ;
        float maxZ = (float)maxPlaneZ;
        float planeZ = (float)sectorPlane.Z;
        float prevPlaneZ = (float)sectorPlane.PrevZ;
        FloodFillInfo floodFillInfo = GetOrCreateFloodFillInfo(sectorPlane);

        for (int i = 0; i < m_freeData.Count; i++)
        {
            if (m_freeData[i].TextureHandle != sectorPlane.TextureHandle)
                continue;

            int key = m_freeData[i].Key;
            m_freeData.RemoveAt(i);
            UpdateStaticWall(key, sectorPlane, vertices, minPlaneZ, maxPlaneZ);
            return key;
        }

        // Zero means "no handle" which the callers use to tell they don't have a handle.
        int newKey = m_floodGeometry.Length + 1;
        var vbo = floodFillInfo.Vertices.Vbo;

        int lightIndex = StaticCacheGeometryRenderer.GetLightBufferIndex(sectorPlane.Sector,
            sectorPlane.Facing == SectorPlaneFace.Floor ? LightBufferType.Floor : LightBufferType.Ceiling);
        m_floodGeometry.Add(new FloodGeometry(newKey, floodFillInfo.TextureHandle, lightIndex, vbo.Count));

        FloodFillVertex topLeft = new((vertices.TopLeft.X, vertices.TopLeft.Y, vertices.TopLeft.Z),
            vertices.TopLeft.Z, planeZ, prevPlaneZ, minZ, maxZ, lightIndex);
        FloodFillVertex topRight = new((vertices.TopRight.X, vertices.TopRight.Y, vertices.TopRight.Z),
            vertices.TopLeft.Z, planeZ, prevPlaneZ, minZ, maxZ, lightIndex);
        FloodFillVertex bottomLeft = new((vertices.BottomLeft.X, vertices.BottomLeft.Y, vertices.BottomLeft.Z),
            vertices.BottomLeft.Z, planeZ, prevPlaneZ, minZ, maxZ, lightIndex);
        FloodFillVertex bottomRight = new((vertices.BottomRight.X, vertices.BottomRight.Y, vertices.BottomRight.Z),
            vertices.BottomLeft.Z, planeZ, prevPlaneZ, minZ, maxZ, lightIndex);
        vbo.Add(topLeft);
        vbo.Add(bottomLeft);
        vbo.Add(topRight);
        vbo.Add(topRight);
        vbo.Add(bottomLeft);
        vbo.Add(bottomRight);

        return newKey;
    }

    public void ClearStaticWall(int floodKey)
    {
        if (TryGetFloodGeometry(floodKey, out var data))
        {
            int listIndex = m_textureHandleToFloodFillInfoIndex[data.TextureHandle];
            FloodFillInfo info = m_floodFillInfos[listIndex];
            OverwriteAndSubUploadVboWithZero(info.Vertices.Vbo, data.VboOffset);
            // Note: We do not delete it because we don't want to track
            // having to compact, re-upload, shuffle things around, etc.
            // This is not ideal since it is a bit wasteful for memory,
            // but the negligible gains are not worth the complexity.
            m_freeData.Add(data);
        }
        else
        {
            Debug.Assert(false, "Trying to clear a flood fill wall that was never added");
        }
    }

    private static void OverwriteAndSubUploadVboWithZero(VertexBufferObject<FloodFillVertex> vbo, int bufferOffset)
    {
        for (int i = 0; i < VerticesPerWall; i++)
        {
            int index = bufferOffset + i;
            vbo.Data.Data[index] = new(Vec3F.Zero, 0, 0, 0, float.MaxValue, float.MinValue, 0);
        }

        vbo.Bind();
        vbo.UploadSubData(bufferOffset, VerticesPerWall);
    }

    public void Render(RenderInfo renderInfo)
    {
        mat4 mvp = Renderer.CalculateMvpMatrix(renderInfo);
        bool drawInvulnerability = false;
        int extraLight = 0;
        float mix = 0.0f;

        if (renderInfo.ViewerEntity.PlayerObj != null)
        {
            if (renderInfo.ViewerEntity.PlayerObj.DrawFullBright())
                mix = 1.0f;
            if (renderInfo.ViewerEntity.PlayerObj.DrawInvulnerableColorMap())
                drawInvulnerability = true;

            extraLight = renderInfo.ViewerEntity.PlayerObj.GetExtraLightRender();
        }

        m_program.Bind();

        GL.ActiveTexture(TextureUnit.Texture0);
        m_program.BoundTexture(TextureUnit.Texture0);
        m_program.SectorLightTexture(TextureUnit.Texture1);
        m_program.Camera(renderInfo.Camera.PositionInterpolated);
        m_program.Mvp(mvp);
        m_program.TimeFrac(renderInfo.TickFraction);
        m_program.HasInvulnerability(drawInvulnerability);
        m_program.MvpNoPitch(Renderer.CalculateMvpMatrix(renderInfo, true));
        m_program.TimeFrac(renderInfo.TickFraction);
        m_program.LightLevelMix(mix);
        m_program.ExtraLight(extraLight);

        for (int i = 0; i < m_floodFillInfos.Count; i++)
        {
            FloodFillInfo info = m_floodFillInfos[i];
            if (info.Vertices.Vbo.Empty)
                continue;

            GLLegacyTexture texture = m_textureManager.GetTexture(info.TextureHandle);
            texture.Bind();

            info.Vertices.Vbo.UploadIfNeeded();
            info.Vertices.Vao.Bind();
            info.Vertices.Vbo.DrawArrays();
        }
    }

    private void DisposeAndClearData()
    {
        foreach (FloodFillInfo info in m_floodFillInfos)
            info.Dispose();
        m_floodFillInfos.Clear();
        m_textureHandleToFloodFillInfoIndex.Clear();
        m_floodGeometry.Clear();
        m_freeData.Clear();
    }

    protected void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        DisposeAndClearData();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}