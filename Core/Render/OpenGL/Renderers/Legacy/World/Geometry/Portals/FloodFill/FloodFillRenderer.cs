using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Util.Configs;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World;
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
public readonly record struct FloodFillKey(double Z, short LightLevel, int TextureIndex, SectorPlaneFace Face, double MinZ, double MaxZ);
public readonly record struct FloodData(FloodFillInfo Info, RenderableVertices<PortalStencilVertex> VertexData);
public readonly record struct FloodFillInfo(SectorPlane SectorPlane, double MinViewZ, double MaxViewZ)
{
    public int GetTextureIndex() => SectorPlane.TextureHandle;
    public SectorPlaneFace GetFace() => SectorPlane.Facing;
    public float GetZ(double frac) => (float)SectorPlane.GetInterpolatedZ(frac);
}

public readonly record struct StaticFloodGeometry(RenderableVertices<PortalStencilVertex> VertexData, int Index, int Length);

readonly record struct SharedProgramUniforms(bool Invul, bool Dropoff, mat4 Mvp, mat4 MvpNoPitch, float LightLevelMix, int ExtraLight);

public class FloodFillRenderer : IDisposable
{
    private readonly IConfig m_config;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly PortalStencilProgram m_stencilProgram = new();
    private readonly FloodFillPlaneProgram m_planeProgram = new();
    private readonly RenderableStaticVertices<FloodFillPlaneVertex> m_planeVertices;
    private readonly Dictionary<FloodFillKey, RenderableVertices<PortalStencilVertex>> m_infoToWorldGeometryVertices = new();
    private readonly List<FloodData> m_floodData = new();
    private readonly Dictionary<int, StaticFloodGeometry> m_floodGeometry = new();
    private int m_floodGeometryKey = 1;
    private bool m_disposed;

    public FloodFillRenderer(IConfig config, LegacyGLTextureManager textureManager)
    {
        m_config = config;
        m_textureManager = textureManager;
        m_planeVertices = new("Flood fill plane", m_planeProgram.Attributes);

        InitializePlaneVbo();
    }

    ~FloodFillRenderer()
    {
        Dispose(false);
    }

    public void UpdateTo(IWorld world)
    {
        m_floodGeometry.Clear();
        DisposeInfoToWorldGeometryVertices();
    }

    private void InitializePlaneVbo()
    {
        // We will assume that these go off into the horizon enough for planes.
        // Maps do not allow us to go beyond [-32768, 32768), so this should be
        // more than enough.
        // This also assumes a flat is always 64 map units.
        const int Coordinate = 8192;
        const float UvCoord = Coordinate / 64f / 2f;

        var vbo = m_planeVertices.Vbo;
        const int MaxValue = 32768;

        for (int xCoord = -MaxValue; xCoord <= MaxValue; xCoord += Coordinate)
        {
            for (int yCoord = -MaxValue; yCoord <= MaxValue; yCoord += Coordinate)
            {
                FloodFillPlaneVertex topLeft = new((xCoord, yCoord + Coordinate), (-UvCoord, -UvCoord));
                FloodFillPlaneVertex topRight = new((xCoord + Coordinate, yCoord + Coordinate), (UvCoord, -UvCoord));
                FloodFillPlaneVertex bottomLeft = new((xCoord, yCoord), (-UvCoord, UvCoord));
                FloodFillPlaneVertex bottomRight = new((xCoord + Coordinate, yCoord), (UvCoord, UvCoord));

                vbo.Add(topLeft);
                vbo.Add(bottomLeft);
                vbo.Add(topRight);
                vbo.Add(topRight);
                vbo.Add(bottomLeft);
                vbo.Add(bottomRight);
            }
        }

        vbo.Bind();
        vbo.Upload();
        vbo.Unbind();
    }

    public int AddStaticWall(SectorPlane sectorPlane, WallVertices vertices, double minViewZ, double maxViewZ)
    {
        FloodFillKey key = new(sectorPlane.Z, sectorPlane.RenderLightLevel, sectorPlane.TextureHandle, sectorPlane.Facing, 
            minViewZ, maxViewZ);

        if (!m_infoToWorldGeometryVertices.TryGetValue(key, out var vertexData))
        {
            string label = $"Static flood fill geometry ({sectorPlane.Id} {sectorPlane.Facing})";
            vertexData = new RenderableStaticVertices<PortalStencilVertex>(label, m_stencilProgram.Attributes);
            m_infoToWorldGeometryVertices[key] = vertexData;

            FloodFillInfo info = new(sectorPlane, minViewZ, maxViewZ);
            m_floodData.Add(new FloodData(info, vertexData));
        }

        PortalStencilVertex topLeft = new(vertices.TopLeft.X, vertices.TopLeft.Y, vertices.TopLeft.Z);
        PortalStencilVertex topRight = new(vertices.TopRight.X, vertices.TopRight.Y, vertices.TopRight.Z);
        PortalStencilVertex bottomLeft = new(vertices.BottomLeft.X, vertices.BottomLeft.Y, vertices.BottomLeft.Z);
        PortalStencilVertex bottomRight = new(vertices.BottomRight.X, vertices.BottomRight.Y, vertices.BottomRight.Z);

        int geometryKey = m_floodGeometryKey++;
        m_floodGeometry[geometryKey] = new(vertexData, vertexData.Vbo.Data.Length, 6);

        vertexData.Vbo.Add(topLeft);
        vertexData.Vbo.Add(bottomLeft);
        vertexData.Vbo.Add(topRight);
        vertexData.Vbo.Add(topRight);
        vertexData.Vbo.Add(bottomLeft);
        vertexData.Vbo.Add(bottomRight);

        return geometryKey;
    }

    public void ClearStaticWall(int key)
    {
        if (!m_floodGeometry.TryGetValue(key, out var geometry))
            return;

        var vbo = geometry.VertexData.Vbo;
        for (int i = 0; i < geometry.Length; i++)
        {
            int index = geometry.Index + i;
            vbo.Data.Data[index] = new PortalStencilVertex(Vec3F.Zero);
        }

        vbo.Bind();
        vbo.UploadSubData(geometry.Index, geometry.Length);

        m_floodGeometry.Remove(key);
    }

    public void Render(RenderInfo renderInfo)
    {
        if (m_floodData.Count == 0)
            return;

        SharedProgramUniforms sharedUniforms = MakeSharedUniforms(renderInfo);

        GL.Enable(EnableCap.StencilTest);
        GL.StencilMask(0xFF);
        GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
        GL.Clear(ClearBufferMask.StencilBufferBit);

        // A smarter way of doing this to avoid tons of program changing would
        // be to draw everything in batches of 255, instead of changing programs
        // twice for every different portal.
        double viewZ = renderInfo.Camera.Position.Z;
        int stencilIndex = 1;
        for (int i = 0; i < m_floodData.Count; i++)
        {
            FloodData floodData = m_floodData[i];
            if (viewZ < floodData.Info.MinViewZ || viewZ > floodData.Info.MaxViewZ)
                continue;

            GL.StencilFunc(StencilFunction.Always, stencilIndex, 0xFF);
            GL.ColorMask(false, false, false, false);
            DrawGeometryWithStencilBits(sharedUniforms.Mvp, stencilIndex, floodData.VertexData.Vbo, floodData.VertexData.Vao);
            GL.ColorMask(true, true, true, true);

            // Culling is disabled only for flood fill rendering because the shader needs
            // to draw the plane's quad whether the camera is above or below it.
            GL.StencilFunc(StencilFunction.Equal, stencilIndex, 0xFF);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            DrawFloodFillPlane(sharedUniforms, floodData.Info, stencilIndex, floodData.Info.SectorPlane.Z);
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

    private SharedProgramUniforms MakeSharedUniforms(RenderInfo renderInfo)
    {
        return new(
            Invul: renderInfo.ViewerEntity.PlayerObj?.DrawInvulnerableColorMap() ?? false,
            Dropoff: m_config.Render.LightDropoff,
            Mvp: Renderer.CalculateMvpMatrix(renderInfo),
            MvpNoPitch: Renderer.CalculateMvpMatrix(renderInfo, true),
            LightLevelMix: (renderInfo.ViewerEntity.PlayerObj?.DrawFullBright() ?? false) ? 1.0f : 0.0f,
            ExtraLight: renderInfo.ViewerEntity.PlayerObj?.GetExtraLightRender() ?? 0
        );
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

    private void DrawFloodFillPlane(in SharedProgramUniforms uniforms, in FloodFillInfo info, int stencilIndex, double z)
    {
        m_planeProgram.Bind();

        GLLegacyTexture texture = m_textureManager.GetTexture(info.GetTextureIndex());
        texture.Bind();

        m_planeProgram.SetZ((float)z);
        m_planeProgram.BoundTexture(TextureUnit.Texture0);
        m_planeProgram.HasInvulnerability(uniforms.Invul);
        m_planeProgram.LightDropoff(uniforms.Dropoff);
        m_planeProgram.Mvp(uniforms.Mvp);
        m_planeProgram.MvpNoPitch(uniforms.MvpNoPitch);
        m_planeProgram.LightLevelMix(uniforms.LightLevelMix);
        m_planeProgram.ExtraLight(uniforms.ExtraLight);
        m_planeProgram.LightLevelFrag(info.SectorPlane.RenderLightLevel);

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
        DisposeInfoToWorldGeometryVertices();

        m_disposed = true;
    }

    private void DisposeInfoToWorldGeometryVertices()
    {
        for (int i = 0; i < m_floodData.Count; i++)
            m_floodData[i].VertexData.Dispose();

        m_infoToWorldGeometryVertices.Clear();
        m_floodData.Clear();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
