using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Util.Configs;
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
public readonly record struct FloodFillInfo(SectorPlane SectorPlane)
{
    public int GetTextureIndex() => SectorPlane.TextureHandle;
    public SectorPlaneFace GetFace() => SectorPlane.Facing;
    public float GetZ(double frac) => (float)SectorPlane.GetInterpolatedZ(frac);
}

readonly record struct SharedProgramUniforms(bool Invul, bool Dropoff, mat4 Mvp, mat4 MvpNoPitch, float LightLevelMix, int ExtraLight);

public class FloodFillRenderer : IDisposable
{
    private readonly IConfig m_config;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly PortalStencilProgram m_stencilProgram = new();
    private readonly FloodFillPlaneProgram m_planeProgram = new();
    private readonly RenderableStaticVertices<FloodFillPlaneVertex> m_planeVertices;
    private readonly Dictionary<FloodFillInfo, RenderableVertices<PortalStencilVertex>> m_infoToWorldGeometryVertices = new();
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
        DisposeInfoToWorldGeometryVertices();
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

        FloodFillPlaneVertex topLeft = new((-Coordinate, Coordinate), (-UVCoord, -UVCoord));
        FloodFillPlaneVertex topRight = new((Coordinate, Coordinate), (UVCoord, -UVCoord));
        FloodFillPlaneVertex bottomLeft = new((-Coordinate, -Coordinate), (-UVCoord, UVCoord));
        FloodFillPlaneVertex bottomRight = new((Coordinate, -Coordinate), (UVCoord, UVCoord));

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

    public void AddStaticWall(SectorPlane sectorPlane, WallVertices vertices)
    {
        FloodFillInfo info = new(sectorPlane);

        if (!m_infoToWorldGeometryVertices.TryGetValue(info, out var vertexData))
        {
            string label = $"Static flood fill geometry ({sectorPlane.Id} {sectorPlane.Facing})";
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
        foreach ((FloodFillInfo info, RenderableVertices<PortalStencilVertex> handles) in m_infoToWorldGeometryVertices)
        {
            double planeZ = info.GetZ(renderInfo.TickFraction);
            if (info.GetFace() == SectorPlaneFace.Ceiling && viewZ > planeZ)
                continue;
            if (info.GetFace() == SectorPlaneFace.Floor && viewZ < planeZ)
                continue;

            GL.StencilFunc(StencilFunction.Always, stencilIndex, 0xFF);
            GL.ColorMask(false, false, false, false);
            DrawGeometryWithStencilBits(sharedUniforms.Mvp, stencilIndex, handles.Vbo, handles.Vao);
            GL.ColorMask(true, true, true, true);

            // Culling is disabled only for flood fill rendering because the shader needs
            // to draw the plane's quad whether the camera is above or below it.
            GL.StencilFunc(StencilFunction.Equal, stencilIndex, 0xFF);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            DrawFloodFillPlane(sharedUniforms, info, stencilIndex, planeZ);
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
        foreach (var renderableObjects in m_infoToWorldGeometryVertices.Values)
            renderableObjects.Dispose();

        m_infoToWorldGeometryVertices.Clear();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
