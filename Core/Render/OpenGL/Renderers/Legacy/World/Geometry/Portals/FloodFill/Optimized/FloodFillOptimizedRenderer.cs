using System;
using System.Collections.Generic;
using System.Diagnostics;
using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.World;
using Helion.World.Geometry.Sectors;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill.Optimized;

public class FloodFillOptimizedRenderer : IDisposable
{
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly FloodFillProgram m_program = new();
    private readonly List<FloodFillInfo> m_floodFillInfos;
    private readonly Dictionary<int, int> m_textureHandleToFloodFillInfoIndex = new();
    private readonly Dictionary<int, (int TextureHandle, int VboOffset)> m_wallToLookupData = new();
    private bool m_disposed;

    public FloodFillOptimizedRenderer(LegacyGLTextureManager textureManager)
    {
        m_textureManager = textureManager;
    }

    ~FloodFillOptimizedRenderer()
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
    
    private FloodFillInfo GetOrCreateFloodFillInfo(SectorPlane plane)
    {
        if (m_textureHandleToFloodFillInfoIndex.TryGetValue(plane.TextureHandle, out int index)) 
            return m_floodFillInfos[index];
        
        FloodFillInfo floodInfo = CreateFloodFillInfo(plane);
        m_textureHandleToFloodFillInfoIndex[plane.TextureHandle] = m_floodFillInfos.Count;
        m_floodFillInfos.Add(floodInfo);
        return floodInfo;
    }
    
    public void AddStaticWall(SectorPlane sectorPlane, int wallId, WallVertices vertices)
    {
        Debug.Assert(!m_wallToLookupData.ContainsKey(wallId), "Trying to add a flood fill wall twice");

        FloodFillInfo floodFillInfo = GetOrCreateFloodFillInfo(sectorPlane);

        var vbo = floodFillInfo.Vertices.Vbo;
        m_wallToLookupData[wallId] = (floodFillInfo.TextureHandle, vbo.Count);
        
        FloodFillVertex topLeft = new((vertices.TopLeft.X, vertices.TopLeft.Y, vertices.TopLeft.Z));
        FloodFillVertex topRight = new((vertices.TopRight.X, vertices.TopRight.Y, vertices.TopRight.Z));
        FloodFillVertex bottomLeft = new((vertices.BottomLeft.X, vertices.BottomLeft.Y, vertices.BottomLeft.Z));
        FloodFillVertex bottomRight = new((vertices.BottomRight.X, vertices.BottomRight.Y, vertices.BottomRight.Z));
        vbo.Add(topLeft);
        vbo.Add(bottomLeft);
        vbo.Add(topRight);
        vbo.Add(topRight);
        vbo.Add(bottomLeft);
        vbo.Add(bottomRight);
    }

    public void ClearStaticWall(int wallId)
    {
        if (m_wallToLookupData.TryGetValue(wallId, out (int TexHandle, int BufferOffset) data))
        {
            int listIndex = m_textureHandleToFloodFillInfoIndex[data.TexHandle];
            FloodFillInfo info = m_floodFillInfos[listIndex];
            OverwriteAndSubUploadVboWithZero(info.Vertices.Vbo, data.BufferOffset);
            // Note: We do not delete it because we don't want to track
            // having to compact, re-upload, shuffle things around, etc.
            // This is not ideal since it is a bit wasteful for memory,
            // but the negligible gains are not worth the complexity.
        }
        else
        {
            Debug.Assert(false, "Trying to clear a flood fill wall that was never added");
        }
    }

    private static void OverwriteAndSubUploadVboWithZero(VertexBufferObject<FloodFillVertex> vbo, int bufferOffset)
    {
        const int VerticesPerWall = 6;

        for (int i = 0; i < VerticesPerWall; i++)
        {
            int index = bufferOffset + i;
            vbo.Data.Data[index] = new(Vec3F.Zero);
        }
        
        vbo.Bind();
        vbo.UploadSubData(bufferOffset, VerticesPerWall);
    }

    public void Render(RenderInfo renderInfo)
    {
        mat4 mvp = Renderer.CalculateMvpMatrix(renderInfo);
        
        m_program.Bind();
        
        GL.ActiveTexture(TextureUnit.Texture0);
        m_program.BoundTexture(TextureUnit.Texture0);
        m_program.Mvp(mvp);
        
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
        m_wallToLookupData.Clear();
    }
    protected virtual void Dispose(bool disposing)
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