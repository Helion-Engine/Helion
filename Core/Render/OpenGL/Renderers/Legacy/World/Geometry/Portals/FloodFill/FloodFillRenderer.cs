using GlmSharp;
using Helion;
using Helion.Geometry.Vectors;
using Helion.Render;
using Helion.Render.OpenGL;
using Helion.Render.OpenGL.Buffer;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Renderers;
using Helion.Render.OpenGL.Renderers.Legacy;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Util.Extensions;
using Helion.World.Geometry.Sectors;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

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
    private readonly RenderableStaticVertices<FloodFillPlaneVertex> m_planeVertices;
    private readonly Dictionary<FloodFillInfo, RenderableVertices<PortalStencilVertex>> m_infoToWorldGeometryVertices = new();
    private bool m_disposed;

    public FloodFillRenderer(LegacyGLTextureManager textureManager)
    {
        m_textureManager = textureManager;
        m_planeVertices = new("Flood fill plane", m_planeProgram.Attributes);

        InitializePlaneVbo();
    }

    ~FloodFillRenderer()
    {
        Dispose(false);
    }

    private void InitializePlaneVbo()
    {
        // We will assume that these go off into the horizon enough for planes.
        // Maps do not allow us to go beyond [-32768, 32768), so this should be
        // more than enough.
        // This also assumes a flat is always 64 map units.
        const float Coordinate = 65536;
        const float UVCoord = Coordinate / 64;

        var vbo = m_planeVertices.Vbo;

        FloodFillPlaneVertex topLeft = new((-Coordinate, Coordinate), (-UVCoord, UVCoord));
        FloodFillPlaneVertex topRight = new((Coordinate, Coordinate), (UVCoord, UVCoord));
        FloodFillPlaneVertex bottomLeft = new((-Coordinate, -Coordinate), (-UVCoord, -UVCoord));
        FloodFillPlaneVertex bottomRight = new((Coordinate, -Coordinate), (UVCoord, -UVCoord));

        vbo.Add(topLeft);
        vbo.Add(bottomLeft);
        vbo.Add(topRight);
        vbo.Add(topRight);
        vbo.Add(bottomLeft);
        vbo.Add(bottomRight);

        vbo.Bind();
        vbo.Upload();
        vbo.Unbind();
    }

    public void AddStaticWall(float z, int textureHandle, SectorPlaneFace face, WallVertices vertices)
    {
        FloodFillInfo info = new(z, textureHandle, face);

        if (!m_infoToWorldGeometryVertices.TryGetValue(info, out var vertexData))
        {
            string label = $"Static flood fill geometry ({z}, {textureHandle} {face})";
            vertexData = new RenderableStaticVertices<PortalStencilVertex>(label, m_stencilProgram.Attributes);
            m_infoToWorldGeometryVertices[info] = vertexData;
        }

        PortalStencilVertex topLeft = new(vertices.TopLeft.X, vertices.TopLeft.Y, vertices.TopLeft.Z);
        PortalStencilVertex topRight = new(vertices.TopRight.X, vertices.TopRight.Y, vertices.TopRight.Z);
        PortalStencilVertex bottomLeft = new(vertices.BottomLeft.X, vertices.BottomLeft.Y, vertices.BottomLeft.Z);
        PortalStencilVertex bottomRight = new(vertices.BottomRight.X, vertices.BottomRight.Y, vertices.BottomRight.Z);

        vertexData.Vbo.Add(topLeft);
        vertexData.Vbo.Add(bottomLeft);
        vertexData.Vbo.Add(topRight);
        vertexData.Vbo.Add(topRight);
        vertexData.Vbo.Add(bottomLeft);
        vertexData.Vbo.Add(bottomRight);
    }

    public void Render(RenderInfo renderInfo)
    {
        if (m_infoToWorldGeometryVertices.Empty())
            return;

        mat4 mvp = Renderer.CalculateMvpMatrix(renderInfo);

        GL.Enable(EnableCap.StencilTest);
        GL.StencilMask(0xFF);
        GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
        GL.Clear(ClearBufferMask.StencilBufferBit);

        // A smarter way of doing this to avoid tons of program changing would
        // be to draw everything in batches of 255, instead of changing programs
        // twice for every different portal.
        int stencilIndex = 1;
        foreach ((FloodFillInfo info, RenderableVertices<PortalStencilVertex> handles) in m_infoToWorldGeometryVertices)
        {
            GL.StencilFunc(StencilFunction.Always, stencilIndex, 0xFF);
            GL.ColorMask(false, false, false, false);
            DrawGeometryWithStencilBits(mvp, stencilIndex, handles.Vbo, handles.Vao);
            GL.ColorMask(true, true, true, true);

            // Culling is disabled only for flood fill rendering because the shader needs
            // to draw the plane's quad whether the camera is above or below it.
            GL.StencilFunc(StencilFunction.Equal, stencilIndex, 0xFF);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            DrawFloodFillPlane(mvp, info, stencilIndex);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

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

    private void DrawGeometryWithStencilBits(in mat4 mvp, int stencilIndex, VertexBufferObject<PortalStencilVertex> vbo, VertexArrayObject vao)
    {
        Debug.Assert(!vbo.Empty, "Why are we making an empty draw call for a portal flood fill (VBO is empty)?");

        m_stencilProgram.Bind();

        m_stencilProgram.SetMvp(mvp);

        vbo.UploadIfNeeded();
        vao.Bind();
        vbo.DrawArrays();
    }

    private void DrawFloodFillPlane(in mat4 mvp, in FloodFillInfo info, int stencilIndex)
    {
        m_planeProgram.Bind();

        GLLegacyTexture texture = m_textureManager.GetTexture(info.TextureIndex);
        texture.Bind();

        m_planeProgram.SetZ(info.Z);
        m_planeProgram.SetTexture(TextureUnit.Texture0);
        m_planeProgram.SetMvp(mvp);

        m_planeVertices.Vao.Bind();
        m_planeVertices.Vbo.DrawArrays();

        m_planeProgram.Unbind();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        m_stencilProgram.Dispose();
        m_planeProgram.Dispose();
        m_planeVertices.Dispose();
        foreach (var renderableObjects in m_infoToWorldGeometryVertices.Values)
            renderableObjects.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
