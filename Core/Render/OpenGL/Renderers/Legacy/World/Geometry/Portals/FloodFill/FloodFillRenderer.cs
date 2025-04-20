using System;
using System.Collections.Generic;
using System.Diagnostics;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Geometry.Sectors;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

public class FloodFillRenderer(LegacyGLTextureManager glTextureManager, FloodFillRenderMode renderMode) : IDisposable
{
    const int FloodPlaneAddCount = 2;
    const int VerticesPerWall = 6;

    private readonly LegacyGLTextureManager m_glTextureManager = glTextureManager;
    private readonly FloodFillRenderMode m_renderMode = renderMode;
    private readonly FloodFillProgram m_program = new();
    private readonly List<FloodFillInfo> m_floodFillInfos = [];
    private readonly Dictionary<int, int> m_textureHandleToFloodFillInfoIndex = [];
    private readonly DynamicArray<FloodGeometry> m_floodGeometry = new();
    private readonly LinkedList<FloodGeometry> m_freeData = [];
    private readonly DynamicArray<LinkedListNode<FloodGeometry>> m_freeNodes = new();
    private TextureManager? m_textureManager;
    private bool m_disposed;

    ~FloodFillRenderer()
    {
        Dispose(false);
    }

    public void UpdateTo(IWorld world)
    {
        m_textureManager = world.ArchiveCollection.TextureManager;
        ClearData();
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


    private FloodGeometry NoGeometry = default;

    private ref FloodGeometry TryGetFloodGeometry(int floodKey, out bool success)
    {
        floodKey--;
        if (floodKey < 0 || floodKey >= m_floodGeometry.Length)
        {            
            success = false;
            return ref NoGeometry;
        }

        success = true;
        return ref m_floodGeometry.Data[floodKey];
    }

    public void UpdateStaticWall(int floodKey, SectorPlane floodPlane, WallVertices vertices, double minPlaneZ, double maxPlaneZ, 
        bool isFloodFillPlane = false)
    {
        if (m_renderMode == FloodFillRenderMode.Dynamic)
        {
            AddStaticWall(floodPlane, vertices, minPlaneZ, maxPlaneZ, isFloodFillPlane);
            return;
        }

        ref var data = ref TryGetFloodGeometry(floodKey, out var success);
        if (!success)
        {
            AddStaticWall(floodPlane, vertices, minPlaneZ, maxPlaneZ, isFloodFillPlane);
            return;
        }

        if (!m_textureHandleToFloodFillInfoIndex.TryGetValue(data.TextureHandle, out int index))
            return;

        FloodFillInfo floodInfo = m_floodFillInfos[index];
        float minZ = (float)minPlaneZ;
        float maxZ = (float)maxPlaneZ;
        float planeZ = (float)floodPlane.Z;
        float prevPlaneZ = (float)floodPlane.PrevZ;

        float topZ = vertices.TopLeft.Z;
        float bottomZ = vertices.BottomRight.Z;
        float prevTopZ = vertices.PrevTopZ;
        float prevBottomZ = vertices.PrevBottomZ;

        FloodFillVertex topLeft = new((vertices.TopLeft.X, vertices.TopLeft.Y, topZ),
            prevTopZ, planeZ, prevPlaneZ, minZ, maxZ, data.LightIndex, data.ColorMapIndex);
        FloodFillVertex topRight = new((vertices.BottomRight.X, vertices.BottomRight.Y, topZ),
            prevTopZ, planeZ, prevPlaneZ, minZ, maxZ, data.LightIndex, data.ColorMapIndex);
        FloodFillVertex bottomLeft = new((vertices.TopLeft.X, vertices.TopLeft.Y, bottomZ),
            prevBottomZ, planeZ, prevPlaneZ, minZ, maxZ, data.LightIndex, data.ColorMapIndex);
        FloodFillVertex bottomRight = new((vertices.BottomRight.X, vertices.BottomRight.Y, bottomZ),
            prevBottomZ, planeZ, prevPlaneZ, minZ, maxZ, data.LightIndex, data.ColorMapIndex);

        var vbo = floodInfo.Vertices.Vbo;
        vbo.Data[data.VboOffset] = topLeft;
        vbo.Data[data.VboOffset + 1] = bottomLeft;
        vbo.Data[data.VboOffset + 2] = topRight;
        vbo.Data[data.VboOffset + 3] = topRight;
        vbo.Data[data.VboOffset + 4] = bottomLeft;
        vbo.Data[data.VboOffset + 5] = bottomRight;

        if (isFloodFillPlane)
            ProjectFloodPlane(vbo, data.VboOffset + VerticesPerWall, vertices, minZ, maxZ, planeZ, prevPlaneZ, data.LightIndex,
                maxPlaneZ > Constants.MaxTextureHeight ? -Constants.MaxTextureHeight : Constants.MaxTextureHeight, false, data.ColorMapIndex);

        vbo.Bind();
        vbo.UploadSubData(data.VboOffset, data.Vertices);
    }

    public int AddStaticWall(SectorPlane sectorPlane, WallVertices vertices, double minPlaneZ, double maxPlaneZ,
        bool isFloodFillPlane = false)
    {
        if (m_textureManager != null && m_textureManager.IsSkyTexture(sectorPlane.TextureHandle))
            return 0;

        int vertexCount = isFloodFillPlane ? (FloodPlaneAddCount + 1) * VerticesPerWall : VerticesPerWall;
        float minZ = (float)minPlaneZ;
        float maxZ = (float)maxPlaneZ;
        float planeZ = (float)sectorPlane.Z;
        float prevPlaneZ = (float)sectorPlane.PrevZ;
        FloodFillInfo floodFillInfo = GetOrCreateFloodFillInfo(sectorPlane);

        int lightIndex, colorMapIndex;
        if (sectorPlane.Facing == SectorPlaneFace.Floor)
        {
            lightIndex = Renderer.GetLightBufferIndex(sectorPlane.Sector, SectorPlaneFace.Floor, LightBufferType.Floor);
            colorMapIndex = Renderer.GetColorMapBufferIndex(sectorPlane.Sector, LightBufferType.Floor);
        }
        else
        {
            lightIndex = Renderer.GetLightBufferIndex(sectorPlane.Sector, SectorPlaneFace.Ceiling, LightBufferType.Ceiling);
            colorMapIndex = Renderer.GetColorMapBufferIndex(sectorPlane.Sector, LightBufferType.Ceiling);
        }

        var flatLightLevel = (byte)Math.Clamp(sectorPlane.LightLevelAbsolute ? sectorPlane.LightLevel : (short)0, (short)0, (short)255);
        colorMapIndex = VertexOptions.ColorMapIndex(colorMapIndex, flatLightLevel);

        for (var node = m_freeData.First; node != null; node = node.Next)
        {
            ref var data = ref node.ValueRef;
            if (data.TextureHandle != sectorPlane.TextureHandle || data.Vertices != vertexCount)
                continue;

            m_freeData.Remove(node);
            m_freeNodes.Add(node);

            m_floodGeometry[data.Key - 1] = new(data.Key, data.TextureHandle, lightIndex, colorMapIndex, data.VboOffset, data.Vertices);
            UpdateStaticWall(data.Key, sectorPlane, vertices, minPlaneZ, maxPlaneZ);
            return data.Key;
        }

        // Zero means "no handle" which the callers use to tell they don't have a handle.
        int newKey = m_floodGeometry.Length + 1;
        var vbo = floodFillInfo.Vertices.Vbo;

        m_floodGeometry.Add(new FloodGeometry(newKey, floodFillInfo.TextureHandle, lightIndex, colorMapIndex, vbo.Count, vertexCount));

        FloodFillVertex topLeft = new((vertices.TopLeft.X, vertices.TopLeft.Y, vertices.TopLeft.Z),
            vertices.TopLeft.Z, planeZ, prevPlaneZ, minZ, maxZ, lightIndex, colorMapIndex);
        FloodFillVertex topRight = new((vertices.BottomRight.X, vertices.BottomRight.Y, vertices.TopLeft.Z),
            vertices.TopLeft.Z, planeZ, prevPlaneZ, minZ, maxZ, lightIndex, colorMapIndex);
        FloodFillVertex bottomLeft = new((vertices.TopLeft.X, vertices.TopLeft.Y, vertices.BottomRight.Z),
            vertices.BottomRight.Z, planeZ, prevPlaneZ, minZ, maxZ, lightIndex, colorMapIndex);
        FloodFillVertex bottomRight = new((vertices.BottomRight.X, vertices.BottomRight.Y, vertices.BottomRight.Z),
            vertices.BottomRight.Z, planeZ, prevPlaneZ, minZ, maxZ, lightIndex, colorMapIndex);

        int offset = vbo.Data.Length;
        int newLength = vbo.Data.Length + VerticesPerWall;
        vbo.Data.EnsureCapacity(newLength);
        vbo.Data[offset++] = topLeft;
        vbo.Data[offset++] = bottomLeft;
        vbo.Data[offset++] = topRight;
        vbo.Data[offset++] = topRight;
        vbo.Data[offset++] = bottomLeft;
        vbo.Data[offset++] = bottomRight;
        vbo.Data.SetLength(newLength);
        vbo.SetNotUploaded();

        if (isFloodFillPlane)
            ProjectFloodPlane(vbo, vbo.Data.Length, vertices, minZ, maxZ, planeZ, prevPlaneZ, lightIndex, 
                maxPlaneZ > Constants.MaxTextureHeight ? -Constants.MaxTextureHeight : Constants.MaxTextureHeight, true, colorMapIndex);

        if (m_renderMode == FloodFillRenderMode.Dynamic)
            return 0;

        return newKey;
    }

    private static unsafe void ProjectFloodPlane(VertexBufferObject<FloodFillVertex> vbo, int startIndex,
       WallVertices vertices, float minZ, float maxZ, float planeZ, float prevPlaneZ, int lightIndex, int addHeight, bool add, int colorMapIndex)
    {
        int newLength = startIndex + FloodPlaneAddCount * VerticesPerWall;
        vbo.Data.EnsureCapacity(newLength);

        var buffer = vbo.Data.Data;
        float currentAddHeight = addHeight;
        for (int i = 0; i < FloodPlaneAddCount; i++)
        {
            float topLeftZ = vertices.TopLeft.Z + currentAddHeight;
            float bottomRightZ = vertices.BottomRight.Z + currentAddHeight;
            float prevTopZ = vertices.PrevTopZ + currentAddHeight;
            float prevBottomZ = vertices.PrevBottomZ + currentAddHeight;

            FloodFillVertex topLeft = new((vertices.TopLeft.X, vertices.TopLeft.Y, topLeftZ),
                prevTopZ, planeZ, prevPlaneZ, minZ, maxZ, lightIndex, colorMapIndex);
            FloodFillVertex topRight = new((vertices.BottomRight.X, vertices.BottomRight.Y, topLeftZ),
                prevTopZ, planeZ, prevPlaneZ, minZ, maxZ, lightIndex, colorMapIndex);
            FloodFillVertex bottomLeft = new((vertices.TopLeft.X, vertices.TopLeft.Y, bottomRightZ),
                prevBottomZ, planeZ, prevPlaneZ, minZ, maxZ, lightIndex, colorMapIndex);
            FloodFillVertex bottomRight = new((vertices.BottomRight.X, vertices.BottomRight.Y, bottomRightZ),
                prevBottomZ, planeZ, prevPlaneZ, minZ, maxZ, lightIndex, colorMapIndex);

            buffer[startIndex++] = topLeft;
            buffer[startIndex++] = bottomLeft;
            buffer[startIndex++] = topRight;
            buffer[startIndex++] = topRight;
            buffer[startIndex++] = bottomLeft;
            buffer[startIndex++] = bottomRight;
            currentAddHeight += addHeight;
        }

        if (add)
            vbo.Data.SetLength(newLength);
    }

    public void ClearStaticWall(int floodKey)
    {
        if (m_renderMode == FloodFillRenderMode.Dynamic)
            return;

        ref var data = ref TryGetFloodGeometry(floodKey, out var success);
        if (success)
        {
            int listIndex = m_textureHandleToFloodFillInfoIndex[data.TextureHandle];
            FloodFillInfo info = m_floodFillInfos[listIndex];
            OverwriteAndSubUploadVboWithZero(info.Vertices.Vbo, data.VboOffset, data.Vertices);

            if (m_freeNodes.Length > 0)
            {
                var node = m_freeNodes.RemoveLast();
                node.Value = data;
                m_freeData.AddLast(node);
            }
            else
            {
                m_freeData.AddLast(new LinkedListNode<FloodGeometry>(data));
            }
        }
        else
        {
            Debug.Assert(false, "Trying to clear a flood fill wall that was never added");
        }
    }

    private static void OverwriteAndSubUploadVboWithZero(VertexBufferObject<FloodFillVertex> vbo, int bufferOffset, int vertices)
    {
        for (int i = 0; i < vertices; i++)
        {
            int index = bufferOffset + i;
            vbo.Data.Data[index] = new(Vec3F.Zero, 0, 0, 0, float.MaxValue, float.MinValue, 0, 0);
        }

        vbo.Bind();
        vbo.UploadSubData(bufferOffset, vertices);
    }

    public void Render(RenderInfo renderInfo)
    {
        m_program.Bind();

        GL.ActiveTexture(BindTextures.BoundTexture);
        m_program.BoundTexture(BindTextures.BoundTexture);
        m_program.SectorLightTexture(BindTextures.SectorLight);
        m_program.ColormapTexture(BindTextures.Colormap);
        m_program.SectorColormapTexture(BindTextures.SectorColormap);
        m_program.Camera(renderInfo.Camera.PositionInterpolated);
        m_program.Mvp(renderInfo.Uniforms.Mvp);
        m_program.TimeFrac(renderInfo.TickFraction);
        m_program.HasInvulnerability(renderInfo.Uniforms.DrawInvulnerability);
        m_program.MvpNoPitch(renderInfo.Uniforms.MvpNoPitch);
        m_program.TimeFrac(renderInfo.TickFraction);
        m_program.LightLevelMix(renderInfo.Uniforms.Mix);
        m_program.ExtraLight(renderInfo.Uniforms.ExtraLight);
        m_program.DistanceOffset(renderInfo.Uniforms.DistanceOffset);
        m_program.ColorMix(renderInfo.Uniforms.ColorMix.Global);
        m_program.PaletteIndex((int)renderInfo.Uniforms.PaletteIndex);
        m_program.ColorMapIndex(renderInfo.Uniforms.ColorMapUniforms.GlobalIndex);
        m_program.LightMode(renderInfo.Uniforms.LightMode);
        m_program.GammaCorrection(renderInfo.Uniforms.GammaCorrection);

        for (int i = 0; i < m_floodFillInfos.Count; i++)
        {
            FloodFillInfo info = m_floodFillInfos[i];
            if (info.Vertices.Vbo.Empty)
                continue;

            GLLegacyTexture texture = m_glTextureManager.GetTexture(info.TextureHandle);
            texture.Bind();

            info.Vertices.Vbo.UploadIfNeeded();
            info.Vertices.Vao.Bind();
            info.Vertices.Vbo.DrawArrays();
        }
    }

    public void ClearVertices()
    {
        Debug.Assert(m_renderMode == FloodFillRenderMode.Dynamic, "Clear should only be called on dynamic flood fill renderer");

        for (int i = 0; i < m_floodFillInfos.Count; i++)
        {
            var info = m_floodFillInfos[i];
            info.Vertices.Vbo.Clear();
        }

        m_floodGeometry.Clear();
    }

    private void ClearData()
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

        ClearData();

        m_program.Dispose();
        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}