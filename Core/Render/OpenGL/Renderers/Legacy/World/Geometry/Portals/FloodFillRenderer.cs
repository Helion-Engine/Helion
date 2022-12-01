using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.World.Geometry.Sectors;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals;

// We want to group every possible piece of wall geometry together into the same
// collection if they have the same Z, texture, and plane face direction. This
// record does the heavy lifting for hashcode and equality implementations so that
// we don't have to.
public record struct FloodFillInfo(float Z, int TextureIndex, SectorPlaneFace Face);

public class FloodFillRenderer : IDisposable
{
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly PortalStencilProgram m_stencilProgram = new();
    private readonly FloodFillPlaneProgram m_planeProgram = new();
    private readonly RenderableStaticVertices<FloodFillPlaneVertex> m_planeVertexInfo;
    private readonly Dictionary<FloodFillInfo, RenderableVertices<PositionVertex>> m_infoToVertexData = new();
    private bool m_disposed;

    public FloodFillRenderer(LegacyGLTextureManager textureManager)
    {
        m_textureManager = textureManager;
        m_planeVertexInfo = new("Flood fill plane", m_planeProgram.Attributes);

        InitializePlaneVbo();
    }

    ~FloodFillRenderer()
    {
        Dispose(false);
    }

    private void InitializePlaneVbo()
    {
        // TODO

        m_planeVertexInfo.Vbo.UploadIfNeeded();
    }

    public void AddStaticWall(float z, int textureHandle, SectorPlaneFace face, WallVertices vertices)
    {
        FloodFillInfo info = new(z, textureHandle, face);

        if (!m_infoToVertexData.TryGetValue(info, out var vertexData))
        {
            string label = $"Static flood fill geometry ({z}, {textureHandle} {face})";
            vertexData = new RenderableStaticVertices<PositionVertex>(label, m_stencilProgram.Attributes);
            m_infoToVertexData[info] = vertexData;
        }

        PositionVertex topLeft = new(vertices.TopLeft.X, vertices.TopLeft.Y, vertices.TopLeft.Z);
        PositionVertex topRight = new(vertices.TopRight.X, vertices.TopRight.Y, vertices.TopRight.Z);
        PositionVertex bottomLeft = new(vertices.BottomLeft.X, vertices.BottomLeft.Y, vertices.BottomLeft.Z);
        PositionVertex bottomRight = new(vertices.BottomRight.X, vertices.BottomRight.Y, vertices.BottomRight.Z);

        vertexData.Vbo.Add(topLeft);
        vertexData.Vbo.Add(bottomLeft);
        vertexData.Vbo.Add(topRight);
        vertexData.Vbo.Add(topRight);
        vertexData.Vbo.Add(bottomLeft);
        vertexData.Vbo.Add(bottomRight);
    }

    public void Render(RenderInfo renderInfo)
    {
        GL.Enable(EnableCap.StencilTest);
        GL.StencilMask(0xFF);
        GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
        GL.Clear(ClearBufferMask.StencilBufferBit);

        // A smarter way of doing this to avoid tons of program changing would
        // be to draw everything in batches of 255, instead of changing programs
        // twice for every different portal.
        int stencilIndex = 1;
        foreach ((FloodFillInfo info, RenderableVertices<PositionVertex> handles) in m_infoToVertexData)
        {
            GL.ColorMask(false, false, false, false);
            GL.StencilFunc(StencilFunction.Always, stencilIndex, 0xFF);
            DrawGeometryWithStencilBits(stencilIndex, renderInfo, handles.Vbo, handles.Vao);

            GL.ColorMask(true, true, true, true);
            GL.StencilFunc(StencilFunction.Equal, stencilIndex, 0xFF);
            GL.Disable(EnableCap.DepthTest);
            DrawFloodFillPlane(info, stencilIndex, renderInfo);
            GL.Enable(EnableCap.DepthTest);

            stencilIndex++;

            // If we somehow draw more than 255 portals, wipe everything and start over.
            // This way we should rarely ever call glClear unless there's a ton of portals.
            if (stencilIndex > 255)
            {
                GL.Clear(ClearBufferMask.StencilBufferBit);
                stencilIndex = 1;
            }
        }

        GL.Disable(EnableCap.StencilTest);
    }

    private void DrawGeometryWithStencilBits(int stencilIndex, RenderInfo renderInfo, VertexBufferObject<PositionVertex> vbo, 
        VertexArrayObject vao)
    {
        Debug.Assert(!vbo.Empty, "Why are we making an empty draw call for a portal flood fill (VBO is empty)?");

        m_stencilProgram.Bind();

        m_stencilProgram.SetMvp(Renderer.CalculateMvpMatrix(renderInfo));

        vbo.UploadIfNeeded();
        vao.Bind();
        vbo.DrawArrays();
    }

    private void DrawFloodFillPlane(in FloodFillInfo info, int stencilIndex, RenderInfo renderInfo)
    {
        m_planeProgram.Bind();

        GLLegacyTexture texture = m_textureManager.GetTexture(info.TextureIndex);
        texture.Bind();

        m_planeProgram.SetZ(info.Z);
        m_planeProgram.SetTexture(TextureUnit.Texture0);
        m_planeProgram.SetMvp(Renderer.CalculateMvpMatrix(renderInfo, true));

        m_planeVertexInfo.Vao.Bind();
        m_planeVertexInfo.Vbo.DrawArrays();

        m_planeProgram.Unbind();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        m_stencilProgram.Dispose();
        m_planeProgram.Dispose();
        m_planeVertexInfo.Dispose();
        foreach (var renderableObjects in m_infoToVertexData.Values)
            renderableObjects.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
