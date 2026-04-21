using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Framebuffer;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Renderers.Legacy.World.Entities;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Renderers.Legacy.World.Primitives;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Decorate.Properties.Enums;
using Helion.Util;
using Helion.Util.Configs;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using NLog;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class LegacyWorldRenderer : WorldRenderer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IConfig m_config;
    private readonly GeometryRenderer m_geometryRenderer;
    private readonly EntityRenderer m_entityRenderer;
    private readonly PrimitiveWorldRenderer m_primitiveRenderer;
    private readonly InterpolationShader m_interpolationProgram = new("Main");
    private readonly InterpolationTransparentShader m_interpolationTransparentProgram = new();
    private readonly InterpolationCompositeShader m_interpolationCompositeProgram = new();
    private readonly InterpolationPlaneClipAlphaShader m_interpolationPlaneClipAlphaProgram = new();
    private readonly InterpolationPlaneClipShaderMrt m_interpolationPlaneClipMrtProgram = new();
    private readonly InterpolationWallClipShader m_interpolationWallClipShader = new();
    private readonly InterpolationWallClipAlphaShader m_interpolationWallClipAlphaProgram = new();
    private readonly StaticShader m_staticProgram = new("Main");
    private readonly StaticPlaneClipShader m_staticPlaneClipProgram = new();
    private readonly StaticPlaneClipAlphaShader m_staticPlaneClipAlphaProgram = new();
    private readonly StaticPlaneClipShaderMrt m_staticPlaneClipMrtProgram = new();
    private readonly StaticWallClipShader m_staticWallClipProgram = new();
    private readonly StaticWallClipAlphaShader m_staticWallClipAlphaProgram = new();
    private readonly StaticTransparentShader m_staticTransparentProgram = new();
    private readonly StaticCompositeShader m_staticCompositeProgram = new();
    private readonly RenderWorldDataManager m_worldDataManager = new();
    private readonly ArchiveCollection m_archiveCollection;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly Stopwatch m_stopwatch = new();
    private readonly OitFrameBuffer m_oitFrameBuffer = new();
    private readonly RenderInfo m_downSizedRenderInfo = new();
    private Vec2D m_occludeViewPos;
    private bool m_occlude;
    private bool m_vanillaRender;
    private bool m_renderStatic;
    private bool m_lastRenderStatic;
    private bool m_pixelGapCorrection;
    private bool m_downscaleVanillaBuffer;
    private bool m_postProcessingEffects;
    private int m_lastTicker = -1;
    private Entity? m_viewerEntity;
    private IWorld? m_previousWorld;
    private RenderBlockMapData m_renderData;
    private PlaneClipFrameBuffer? m_planeClipFrameBuffer;
    private PlaneClipFrameBuffer? m_wallClipFrameBuffer;

    public LegacyWorldRenderer(IConfig config, ArchiveCollection archiveCollection, LegacyGLTextureManager textureManager)
    {
        m_config = config;
        m_entityRenderer = new(config, textureManager, archiveCollection);
        m_primitiveRenderer = new();
        m_geometryRenderer = new(config, archiveCollection, textureManager, m_interpolationProgram, m_staticProgram, m_worldDataManager);
        m_archiveCollection = archiveCollection;
        m_textureManager = textureManager;
    }

    ~LegacyWorldRenderer()
    {
        ReleaseUnmanagedResources();
    }

    public override void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public override void UpdateToNewWorld(IWorld world)
    {
        m_stopwatch.Restart();
        m_vanillaRender = m_config.Render.VanillaRender;
        TransferHeights.FlushSectorReferences();
        m_lastRenderedWorld.SetTarget(world);

        if (m_vanillaRender && m_planeClipFrameBuffer == null)
            m_planeClipFrameBuffer = new();
        else
            m_planeClipFrameBuffer?.Dispose();

        if (m_vanillaRender && m_wallClipFrameBuffer == null)
            m_wallClipFrameBuffer = new();
        else
            m_wallClipFrameBuffer?.Dispose();

        if (m_previousWorld != null)
            m_previousWorld.OnResetInterpolation -= World_OnResetInterpolation;

        var spriteDefinitions = m_archiveCollection.TextureManager.SpriteDefinitions;
        for (int i = 0; i < spriteDefinitions.Length; i++)
        {
            var spriteDefinition = spriteDefinitions[i];
            if (spriteDefinition == null)
                continue;

            m_textureManager.CacheSpriteRotations(spriteDefinition);
        }

        m_geometryRenderer.UpdateTo(world);
        m_entityRenderer.UpdateTo(world);
        world.OnResetInterpolation += World_OnResetInterpolation;
        m_previousWorld = world;
        m_lastTicker = -1;
        m_pixelGapCorrection = m_config.Render.PixelGapCorrection.Value;

        m_stopwatch.Stop();
        Log.Info($"Completed level geometry {m_stopwatch.Elapsed}");
    }

    private void World_OnResetInterpolation(object? sender, EventArgs e)
    {
        m_lastTicker = -1;
        ResetInterpolation((IWorld)sender!);
    }

    private void IterateBlockmap(IWorld world, RenderInfo renderInfo)
    {
        bool shouldRender = m_lastTicker != world.GameTicker || m_renderStatic != m_lastRenderStatic;
        if (!shouldRender)
            return;

        m_geometryRenderer.SetRenderMode(m_renderStatic ? GeometryRenderMode.Dynamic : GeometryRenderMode.All, renderInfo.TransferHeightView);

        m_renderData.ViewerEntity = renderInfo.ViewerEntity;
        m_renderData.ViewPosInterpolated = renderInfo.Camera.PositionInterpolated.XY.Double;
        m_renderData.ViewPosInterpolated3D = renderInfo.Camera.PositionInterpolated.Double;
        m_renderData.ViewPos3D = renderInfo.Camera.Position.Double;
        m_renderData.ViewDirection = renderInfo.Camera.Direction.XY.Double;
        m_renderData.ViewIsland = world.Geometry.IslandGeometry.Islands[world.Geometry.SubsectorToIslandId[renderInfo.ViewerEntity.SubsectorId]];

        m_viewerEntity = renderInfo.ViewerEntity;
        m_geometryRenderer.Clear(renderInfo.TickFraction, true);
        m_renderData.CheckCount = ++WorldStatic.CheckCounter;

        m_renderData.MaxDistance = renderInfo.Uniforms.MaxDistance;

        m_renderData.MaxDistanceSquared = m_renderData.MaxDistance * m_renderData.MaxDistance;
        m_renderData.OccludePos = m_occlude ? m_occludeViewPos : null;
        Box2D box = new(m_renderData.ViewPosInterpolated.X, m_renderData.ViewPosInterpolated.Y, m_renderData.MaxDistance);

        Vec2D occluder = m_renderData.OccludePos ?? Vec2D.Zero;
        bool occlude = m_renderData.OccludePos.HasValue;

        var it = world.RenderBlockmap.CreateBoxIteration(box);
        var dimension = world.RenderBlockmap.Dimension;
        var origin = world.RenderBlockmap.Origin;
        for (int by = it.BlockStartY; by <= it.BlockEndY; by++)
        {
            for (int bx = it.BlockStartX; bx <= it.BlockEndX; bx++)
            {
                var index = by * it.Width + bx;
                if (occlude && !BlockInView(bx, by, dimension, origin, occluder, m_renderData.ViewDirection))
                    continue;

                RenderSectors(world, index);

                if (m_renderStatic)
                    RenderSides(world, index);

                int renderIndex = 0;
                for (var entity = world.RenderBlockmap.HeadRenderEntities[index]; entity != null; entity = entity.RenderBlockNext)
                    RenderEntity(world, entity, renderIndex++);
            }
        }

        m_lastTicker = world.GameTicker;
    }

    private static bool BlockInView(int x, int y, int dimension, in Vec2D origin, in Vec2D viewPos, in Vec2D viewDirection)
    {
        double minX = x * dimension + origin.X;
        double minY = y * dimension + origin.Y;
        double maxX = minX + dimension;
        double maxY = minY + dimension;

        Vec2D p1 = new(minX - viewPos.X, minY - viewPos.Y);
        Vec2D p2 = new(maxX - viewPos.X, maxY - viewPos.Y);
        Vec2D p3 = new(minX - viewPos.X, maxY - viewPos.Y);
        Vec2D p4 = new(maxX - viewPos.X, minY - viewPos.Y);
        return p1.Dot(viewDirection) >= 0 || p2.Dot(viewDirection) >= 0 || p3.Dot(viewDirection) >= 0 || p4.Dot(viewDirection) >= 0;
    }

    private void RenderSectors(IWorld world, int blockIndex)
    {
        var sectorList = m_renderStatic ? world.RenderBlockmap.DynamicSectors[blockIndex] : world.RenderBlockmap.Sectors[blockIndex];
        if (sectorList == null)
            return;

        for (var islandNode = sectorList.Head; islandNode != null; islandNode = islandNode.Next)
        {
            var sectorIsland = islandNode.Value.Island;
            var sector = islandNode.Value.Sector;
            // Multiple 3D sectors can link in the same island so this check can't short here.
            // The island node's sector is the fake 3D sector so using the CheckCount on it is valid below.
            if (sector.Sector3D == null && sector.Sectors3D.Length == 0 && sectorIsland.BlockmapCount == m_renderData.CheckCount)
                continue;

            sectorIsland.BlockmapCount = m_renderData.CheckCount;
            if (sectorIsland.ParentIsland != null && sectorIsland.ParentIsland != m_renderData.ViewIsland)
                continue;

            if (sector.CheckCount == m_renderData.CheckCount)
                continue;

            var dx1 = Math.Max(sectorIsland.Box.Min.X - m_renderData.ViewPosInterpolated.X, Math.Max(0, m_renderData.ViewPosInterpolated.X - sectorIsland.Box.Max.X));
            var dy1 = Math.Max(sectorIsland.Box.Min.Y - m_renderData.ViewPosInterpolated.Y, Math.Max(0, m_renderData.ViewPosInterpolated.Y - sectorIsland.Box.Max.Y));
            if (dx1 * dx1 + dy1 * dy1 <= m_renderData.MaxDistanceSquared)
            {
                m_geometryRenderer.RenderSector(sector, m_renderData.ViewPos3D, m_renderData.ViewPosInterpolated3D);
                sector.CheckCount = m_renderData.CheckCount;
            }
        }
    }

    private void RenderSides(IWorld world, int blockIndex)
    {
        var sides = world.RenderBlockmap.DynamicSides[blockIndex];
        if (sides == null)
            return;

        // DynamicSides are scrolling textures and should not setup cover walls.
        m_geometryRenderer.SetBufferCoverWall(false);
        for (int i = 0; i < sides.Length; i++)
        {
            var side = sides.Data[i];
            if (side.BlockmapCount == m_renderData.CheckCount)
                continue;
            if (side.Sector.IsMoving || (side.PartnerSide != null && side.PartnerSide.Sector.IsMoving))
                continue;

            side.BlockmapCount = m_renderData.CheckCount;
            m_geometryRenderer.RenderSectorWall(side.Sector, side.Line, m_renderData.ViewPos3D, m_renderData.ViewPosInterpolated3D);
        }
        m_geometryRenderer.SetBufferCoverWall(true);
    }

    void RenderEntity(IWorld world, Entity entity, int renderIndex)
    {
        if (entity.FrameState.Frame.IsInvisible || entity.Flags.Invisible() || entity.Flags.NoSector() || entity == m_viewerEntity || entity.Properties.RenderStyle == RenderStyle.None)
            return;

        // Not in front 180 FOV
        if (m_renderData.OccludePos.HasValue)
        {
            Vec2D entityToTarget = new(entity.Position.X - m_renderData.OccludePos.Value.X, entity.Position.Y - m_renderData.OccludePos.Value.Y);
            if (entityToTarget.Dot(m_renderData.ViewDirection) < 0)
                return;
        }

        double dx = Math.Max(entity.Position.X - m_renderData.ViewPosInterpolated.X, Math.Max(0, m_renderData.ViewPosInterpolated.X - entity.Position.X));
        double dy = Math.Max(entity.Position.Y - m_renderData.ViewPosInterpolated.Y, Math.Max(0, m_renderData.ViewPosInterpolated.Y - entity.Position.Y));
        entity.RenderDistanceSquared = dx * dx + dy * dy;
        if (entity.RenderDistanceSquared > m_renderData.MaxDistanceSquared)
            return;

        entity.LastRenderGametick = world.Gametick;
        m_entityRenderer.RenderEntity(entity, m_renderData.ViewPosInterpolated, renderIndex);     
    }

    protected override void PerformRender(IWorld world, RenderInfo renderInfo, GLFramebuffer framebuffer)
    {
        // If the transfer height view is not the middle then the cached static geometry cannot be used.
        // Render all sectors dynamically instead.
        m_lastRenderStatic = m_renderStatic;
        m_renderStatic = renderInfo.TransferHeightView == TransferHeightView.Middle;
        m_postProcessingEffects = m_config.Render.PostProcessingEffects;
        Clear(world, renderInfo);

        if (framebuffer.DepthTexture == null)
            throw new Exception("Framebuffer must have a depth texture.");

        var dimension = new Dimension(renderInfo.Viewport.Width, renderInfo.Viewport.Height);
        m_oitFrameBuffer.CreateOrUpdate(dimension, framebuffer.DepthTexture);

        var prevDownscale = m_downscaleVanillaBuffer;
        m_downscaleVanillaBuffer = m_config.Render.DownScaleVanillaRenderSampleBuffer.Value > 1;
        SetupClipBuffers(framebuffer, dimension, prevDownscale != m_downscaleVanillaBuffer);

        if (m_lastTicker != world.GameTicker)
            m_entityRenderer.Start(renderInfo);

        SetOccludePosition(renderInfo.Camera.PositionInterpolated.Double, renderInfo.Camera.YawRadians, renderInfo.Camera.PitchRadians,
            ref m_occlude, ref m_occludeViewPos);
        IterateBlockmap(world, renderInfo);
        PopulatePrimitives(world);

        m_geometryRenderer.RenderSkies(renderInfo);
        RenderFloodFill(renderInfo);

        if (m_renderStatic)
            m_geometryRenderer.RenderStaticSkies(renderInfo);

        m_primitiveRenderer.Render(renderInfo);

        if (!m_vanillaRender)
        {
            m_interpolationProgram.Bind();
            GL.ActiveTexture(BindTextures.BoundTexture);
            SetInterpolationUniforms(m_interpolationProgram, renderInfo);
            m_interpolationProgram.VertexGapClampUV(m_pixelGapCorrection);
            m_worldDataManager.RenderWalls();
            m_interpolationProgram.VertexGapClampUV(false);
            m_worldDataManager.RenderFlats();

            if (m_renderStatic)
            {
                m_staticProgram.Bind();
                GL.ActiveTexture(BindTextures.BoundTexture);
                SetStaticUniforms(m_staticProgram, renderInfo);
                m_staticProgram.VertexGapClampUV(m_pixelGapCorrection);
                m_geometryRenderer.RenderStaticGeometryWalls();
                m_staticProgram.VertexGapClampUV(false);
                m_geometryRenderer.RenderStaticGeometryFlats();
            }

            RenderTwoSidedMiddleWalls(renderInfo);
            m_entityRenderer.RenderOpaque(renderInfo);
            RenderTransparent(renderInfo, framebuffer);
            return;
        }

        m_interpolationProgram.Bind();
        GL.ActiveTexture(BindTextures.BoundTexture);
        SetInterpolationUniforms(m_interpolationProgram, renderInfo);
        m_interpolationProgram.VertexGapClampUV(m_pixelGapCorrection);
        m_worldDataManager.RenderWalls();

        if (m_downscaleVanillaBuffer)
        {
            m_interpolationProgram.VertexGapClampUV(false);
            m_worldDataManager.RenderFlats();
        }

        if (m_renderStatic)
        {
            m_staticProgram.Bind();
            GL.ActiveTexture(BindTextures.BoundTexture);
            SetStaticUniforms(m_staticProgram, renderInfo);
            m_staticProgram.VertexGapClampUV(m_pixelGapCorrection);
            m_geometryRenderer.RenderStaticGeometryWalls();

            if (m_downscaleVanillaBuffer)
            {
                m_staticProgram.VertexGapClampUV(false);
                m_geometryRenderer.RenderStaticGeometryFlats();
            }
        }

        RenderTwoSidedMiddleWalls(renderInfo);

        if (m_wallClipFrameBuffer != null || m_planeClipFrameBuffer != null)
            WriteSpriteClipBuffers(renderInfo, framebuffer);

        GL.Clear(ClearBufferMask.DepthBufferBit);

        m_entityRenderer.RenderOpaque(renderInfo);
        RenderTransparent(renderInfo, framebuffer);
    }

    private void RenderFloodFill(RenderInfo renderInfo)
    {
        // Doom would draw middle textures over flood fill.
        // Setting the factor using PolygonOffset will push them further away in depth so middle textures are closer and render over.
        // Very tiny for reversed z. Flood fill is pushed in world coordinates in the shader.
        GL.Enable(EnableCap.PolygonOffsetFill);
        SetPolygonOffsetFloodFill();
        m_geometryRenderer.RenderPortals(renderInfo);
        GL.Disable(EnableCap.PolygonOffsetFill);
    }

    private static void SetPolygonOffsetFloodFill()
    {
        if (ShaderVars.ReversedZ)
            GL.PolygonOffset(-0.005f, -0.002f);
        else
            GL.PolygonOffset(0.05f, 1f);
    }

    private void WriteSpriteClipBuffers(RenderInfo renderInfo, GLFramebuffer framebuffer)
    {
        var useRenderInfo = renderInfo;
        if (renderInfo.Uniforms.DownScaleAmount > 1)
        {
            var downScaleAmount = renderInfo.Uniforms.DownScaleAmount;
            useRenderInfo = m_downSizedRenderInfo;
            var viewport = renderInfo.Viewport;
            viewport = new Rectangle((int)(viewport.X / downScaleAmount), (int)(viewport.Y / downScaleAmount), (int)(viewport.Width / downScaleAmount), (int)(viewport.Height / downScaleAmount));
            m_downSizedRenderInfo.Set(renderInfo.Camera, renderInfo.TickFraction, viewport, renderInfo.ViewerEntity,
                renderInfo.DrawAutomap, renderInfo.AutomapOffset, renderInfo.AutomapScale,
                renderInfo.Config, renderInfo.ViewSector, renderInfo.TransferHeightView);
            m_downSizedRenderInfo.Uniforms = Renderer.GetShaderUniforms(m_config, m_downSizedRenderInfo);

            GL.Viewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
        }

        if (m_planeClipFrameBuffer != null)
            WritePlaneClipData(m_planeClipFrameBuffer, useRenderInfo, framebuffer, false);

        if (m_wallClipFrameBuffer != null)
            WritePlaneClipData(m_wallClipFrameBuffer, useRenderInfo, framebuffer, true);

        if (renderInfo.Uniforms.DownScaleAmount > 1)
            GL.Viewport(renderInfo.Viewport.X, renderInfo.Viewport.Y, renderInfo.Viewport.Width, renderInfo.Viewport.Height);
    }

    private void SetupClipBuffers(GLFramebuffer framebuffer, Dimension dimension, bool downscaleChanged)
    {
        bool bind = false;
        if (m_planeClipFrameBuffer != null)
        {
            var colorBuffer = m_downscaleVanillaBuffer ? null : framebuffer;
            m_planeClipFrameBuffer.CreateOrUpdate("PlaneClip", PlaneClipType.Plane, dimension, colorBuffer, downscaleChanged);
            m_planeClipFrameBuffer.BindFrameBuffer();
            m_planeClipFrameBuffer.Clear();
            bind = true;
        }

        if (m_wallClipFrameBuffer != null)
        {
            m_wallClipFrameBuffer.CreateOrUpdate("WallClip", PlaneClipType.Wall, dimension, null, false);
            m_wallClipFrameBuffer.BindFrameBuffer();
            m_wallClipFrameBuffer.Clear();
            bind = true;
        }

        if (bind)
            framebuffer.Bind();
    }

    private void WritePlaneClipData(PlaneClipFrameBuffer planeClipFrameBuffer, RenderInfo renderInfo, GLFramebuffer framebuffer, bool walls)
    {
        planeClipFrameBuffer.BindFrameBuffer();
        PlaneClipFrameBuffer.StartRender();
        GL.Disable(EnableCap.Blend);

        if (walls)
        {
            m_interpolationWallClipShader.Bind();
            GL.ActiveTexture(BindTextures.BoundTexture);
            SetInterpolationUniforms(m_interpolationWallClipShader, renderInfo);
            GL.Disable(EnableCap.CullFace);
            m_worldDataManager.RenderCoverWalls();
            m_geometryRenderer.RenderWallClipPortals(renderInfo);
            GL.Enable(EnableCap.CullFace);
            m_interpolationWallClipShader.Unbind();

            m_interpolationWallClipAlphaProgram.Bind();
            SetInterpolationUniforms(m_interpolationWallClipAlphaProgram, renderInfo);

            if (WorldStatic.Sector3D)
            {
                GL.CullFace(TriangleFace.Front);
                m_worldDataManager.RenderMiddle3D();
                GL.CullFace(TriangleFace.Back);
                m_worldDataManager.RenderMiddle3D();
            }
            
            m_worldDataManager.RenderTwoSidedMiddleWalls();
        }
        else
        {
            InterpolationShader program = m_downscaleVanillaBuffer ? 
                (WorldStatic.Sector3D ? m_interpolationPlaneClipAlphaProgram : m_interpolationPlaneClipAlphaProgram) : m_interpolationPlaneClipMrtProgram;
            program.Bind();
            GL.ActiveTexture(BindTextures.BoundTexture);
            SetInterpolationUniforms(program, renderInfo);
            m_worldDataManager.RenderFlats();
        }

        if (m_renderStatic)
        {
            if (walls)
            {
                m_staticWallClipProgram.Bind();
                GL.ActiveTexture(BindTextures.BoundTexture);
                SetStaticUniforms(m_staticWallClipProgram, renderInfo);
                m_geometryRenderer.RenderStaticOneSidedCoverWalls();
                m_geometryRenderer.RenderStaticCoverWalls();
                GL.CullFace(TriangleFace.Front);
                m_geometryRenderer.RenderStaticCoverWalls();
                GL.CullFace(TriangleFace.Back);
                m_staticWallClipProgram.Unbind();

                m_staticWallClipAlphaProgram.Bind();
                SetStaticUniforms(m_staticWallClipAlphaProgram, renderInfo);

                if (WorldStatic.Sector3D)
                {                    
                    GL.CullFace(TriangleFace.Front);
                    m_geometryRenderer.RenderStaticMiddle3D();
                    GL.CullFace(TriangleFace.Back);
                    m_geometryRenderer.RenderStaticMiddle3D();
                }
                
                m_geometryRenderer.RenderStaticTwoSidedWalls();
            }
            else
            {
                StaticShader program = m_downscaleVanillaBuffer ? 
                    (WorldStatic.Sector3D ? m_staticPlaneClipAlphaProgram : m_staticPlaneClipProgram) : m_staticPlaneClipMrtProgram;
                program.Bind();
                GL.ActiveTexture(BindTextures.BoundTexture);
                program.VertexGapClampUV(false);
                SetStaticUniforms(program, renderInfo);
                m_geometryRenderer.RenderStaticGeometryFlats();
            }
        }

        PlaneClipFrameBuffer.UnbindFrameBuffer();
        framebuffer.Bind();
        planeClipFrameBuffer.BindPlaneTexture(walls ? BindTextures.WallClipTexture : BindTextures.PlaneClipTexture);
        ResetBlendEquations();
    }

    private void RenderTransparent(RenderInfo renderInfo, GLFramebuffer framebuffer)
    {
        var hasEntityFuzzData = m_entityRenderer.HasDataToRenderByStyle(RenderDataStyle.Fuzzy); 
        var hasEntityAlphaData = m_entityRenderer.HasAlphaToRender();
        var hasDynamicAlphaGeometry = m_worldDataManager.HasAlphaToRender();
        var hasStaticAlphaGeometry = m_geometryRenderer.StaticRenderer.HasAlphaToRender();
        if (!hasEntityFuzzData && !hasEntityAlphaData && !hasDynamicAlphaGeometry && !hasStaticAlphaGeometry)
            return;

        SetPolygonOffsetFloodFill();
        m_oitFrameBuffer.StartRender();
        GL.DepthMask(false);

        if (hasEntityAlphaData || hasEntityFuzzData)
            m_entityRenderer.RenderOitTransparentPass(renderInfo);

        m_oitFrameBuffer.BindTextures(BindTextures.AccumTexture, BindTextures.AccumCountTexture, BindTextures.FuzzTexture, BindTextures.OpaqueTexture, framebuffer);

        if (hasEntityFuzzData)
        {
            if (m_postProcessingEffects)
            {
                if (GLInfo.MemoryBarrierSupported)
                    GL.MemoryBarrier(MemoryBarrierFlags.FramebufferBarrierBit);

                ResetBlendEquations();
                framebuffer.Bind();
                // Refract pixels in the opaque framebuffer
                m_entityRenderer.RenderOitFuzzRefractionPass(renderInfo, false);
            }

            OitFrameBuffer.SetBlendEquations();
            m_oitFrameBuffer.BindFrameBuffer();

            m_entityRenderer.RenderOitTransparentFuzzPass(renderInfo);
        }

        if (hasDynamicAlphaGeometry)
        {
            m_interpolationTransparentProgram.Bind();
            // Alpha walls are the exception to pixel gap correction. Only required for opaque walls.
            m_interpolationTransparentProgram.VertexGapClampUV(false);
            SetInterpolationUniforms(m_interpolationTransparentProgram, renderInfo);
            GL.ActiveTexture(BindTextures.BoundTexture);
            m_worldDataManager.RenderAllAlpha();

            if (m_worldDataManager.HasStyleToRender(RenderDataStyle.FogBarrier))
            {
                m_interpolationTransparentProgram.FogBarrier(true);
                GL.Enable(EnableCap.PolygonOffsetFill);
                m_worldDataManager.Render(RenderDataStyle.FogBarrier);
                GL.Disable(EnableCap.PolygonOffsetFill);
            }
        }

        if (hasStaticAlphaGeometry)
        {
            m_staticTransparentProgram.Bind();
            m_staticTransparentProgram.VertexGapClampUV(false);
            SetStaticUniforms(m_staticTransparentProgram, renderInfo);
            GL.ActiveTexture(BindTextures.BoundTexture);
            m_geometryRenderer.StaticRenderer.RenderAllAlpha();

            if (m_geometryRenderer.StaticRenderer.HasStyleToRender(RenderDataStyle.FogBarrier))
            {
                m_staticTransparentProgram.FogBarrier(true);
                GL.Enable(EnableCap.PolygonOffsetFill);
                m_geometryRenderer.StaticRenderer.Render(RenderDataStyle.FogBarrier);
                GL.Disable(EnableCap.PolygonOffsetFill);
            }
        }

        ResetBlendEquations();
        framebuffer.Bind();

        if (hasEntityAlphaData || hasEntityFuzzData)
        {
            m_entityRenderer.StartRenderOitCompositePass(renderInfo);
            RenderCompositeStyles(m_entityRenderer);
        }

        SetBlendEquation(RenderDataStyle.Normal);

        if (hasDynamicAlphaGeometry)
        {
            m_interpolationCompositeProgram.Bind();
            m_interpolationCompositeProgram.VertexGapClampUV(false);
            SetInterpolationUniforms(m_interpolationCompositeProgram, renderInfo);
            GL.ActiveTexture(BindTextures.BoundTexture);
            RenderCompositeStyles(m_worldDataManager);
            SetBlendEquation(RenderDataStyle.Normal);
        }

        if (hasStaticAlphaGeometry)
        {
            m_staticCompositeProgram.Bind();
            m_staticCompositeProgram.VertexGapClampUV(false);
            SetStaticUniforms(m_staticCompositeProgram, renderInfo);
            GL.ActiveTexture(BindTextures.BoundTexture);
            RenderCompositeStyles(m_geometryRenderer.StaticRenderer);
            SetBlendEquation(RenderDataStyle.Normal);
        }

        if (hasEntityFuzzData)
            m_entityRenderer.RenderOitFuzzRefractionPass(renderInfo, true);

        GL.DepthMask(true);
    }

    private void RenderCompositeStyles(IStyleRenderer styleRenderer)
    {
        styleRenderer.Render(RenderDataStyle.Translucent);

        var hasFogBarrier = styleRenderer.HasStyleToRender(RenderDataStyle.FogBarrier);
        if (styleRenderer.HasStyleToRender(RenderDataStyle.Add) || hasFogBarrier)
        {
            SetBlendEquation(RenderDataStyle.Add);
            styleRenderer.Render(RenderDataStyle.Add);

            //if (hasFogBarrier)
            //{
            //    GL.Enable(EnableCap.PolygonOffsetFill);
            //    styleRenderer.Render(RenderDataStyle.FogBarrier);
            //    GL.Disable(EnableCap.PolygonOffsetFill);
            //}
        }

        if (styleRenderer.HasStyleToRender(RenderDataStyle.ColorAdd))
        {
            SetBlendEquation(RenderDataStyle.ColorAdd);
            styleRenderer.Render(RenderDataStyle.ColorAdd);
        }

        if (hasFogBarrier)
        {
            m_staticCompositeProgram.FogBarrier(true);
            SetBlendEquation(RenderDataStyle.Translucent);
            GL.Enable(EnableCap.PolygonOffsetFill);
            styleRenderer.Render(RenderDataStyle.FogBarrier);
            GL.Disable(EnableCap.PolygonOffsetFill);
        }
    }

    public static void SetBlendEquation(RenderDataStyle style)
    {
        switch(style)
        {
            case RenderDataStyle.Add:
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                break;
            case RenderDataStyle.ColorAdd:
                GL.BlendFunc(BlendingFactor.SrcColor, BlendingFactor.One);
                break;
            default:
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
        }
    }

    public static void ResetBlendEquations()
    {
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    private void RenderTwoSidedMiddleWalls(RenderInfo renderInfo)
    {
        m_interpolationProgram.Bind();
        GL.ActiveTexture(BindTextures.BoundTexture);
        m_interpolationProgram.VertexGapClampUV(m_pixelGapCorrection);
        m_worldDataManager.RenderTwoSidedMiddleWalls();

        if (WorldStatic.Sector3D)
            m_worldDataManager.RenderMiddle3D();

        if (m_renderStatic)
        {
            m_staticProgram.Bind();
            GL.ActiveTexture(BindTextures.BoundTexture);
            SetStaticUniforms(m_staticProgram, renderInfo);
            m_staticProgram.VertexGapClampUV(m_pixelGapCorrection);
            m_geometryRenderer.RenderStaticTwoSidedWalls();

            if (WorldStatic.Sector3D)
                m_geometryRenderer.RenderStaticMiddle3D();
        }
    }

    public override void ResetInterpolation(IWorld world)
    {
        m_entityRenderer.ResetInterpolation(world);
    }

    public static void SetOccludePosition(in Vec3D position, double angleRadians, double pitchRadians, 
        ref bool occlude, ref Vec2D occludeViewPos)
    {
        // This is a hack until frustum culling exists.
        // Push the position back to stop occluding things that are straight up/down
        if (Math.Abs(pitchRadians) > MathHelper.QuarterPi)
        {
            occlude = false;
            return;
        }

        occlude = true;
        Vec2D unit = Vec2D.UnitCircle(angleRadians + MathHelper.Pi);
        occludeViewPos = position.XY + (unit * 32);
    }

    private void Clear(IWorld world, RenderInfo renderInfo)
    {
        bool newTick = world.GameTicker != m_lastTicker;
        m_geometryRenderer.Clear(renderInfo.TickFraction, newTick);

        if (newTick)
        {
            m_entityRenderer.Clear(world);
            m_worldDataManager.Clear();
        }
    }

    private void PopulatePrimitives(IWorld world)
    {
        var node = world.Player.Tracers.Tracers.First;
        while (node != null)
        {
            var info = node.Value;
            int ticks = info.Ticks <= 0 ? 0 : world.Gametick - info.Gametick;
            if (ticks > info.Ticks)
            {
                var removeNode = node;
                node = node.Next;
                world.Player.Tracers.Tracers.Remove(removeNode);
                continue;
            }
        
            float alpha = ticks == 0 ? 1 : (info.Ticks - ticks) / (float)ticks;
            for (var i = 0; i < info.Segs.Count; i++)
            {
                Seg3D tracer = info.Segs[i];
                AddSeg(tracer, node.Value.Color, alpha, info.Type);
            }

            node = node.Next;
        }
    }

    void AddSeg(Seg3D segment, Vec3F color, float alpha, PrimitiveRenderType type)
    {
        Seg3F seg = (segment.Start.Float, segment.End.Float);
        m_primitiveRenderer.AddSegment(seg, color, alpha, type);
    }

    private void SetInterpolationUniforms(InterpolationShader program, RenderInfo renderInfo)
    {
        program.Bind();
        program.BoundTexture(BindTextures.BoundTexture);
        program.SectorLightTexture(BindTextures.SectorLight);
        program.ColormapTexture(BindTextures.Colormap);
        program.SectorColormapTexture(BindTextures.SectorColormap);
        program.SectorFogTexture(BindTextures.SectorFog);
        program.BrightmapTexture(BindTextures.BrightmapTexture);
        program.PlaneClipTexture(BindTextures.PlaneClipTexture);
        program.WallClipTexture(BindTextures.WallClipTexture);
        program.HasInvulnerability(renderInfo.Uniforms.DrawInvulnerability);
        program.Mvp(renderInfo.Uniforms.Mvp);
        program.MvpNoPitch(renderInfo.Uniforms.MvpNoPitch);
        program.TimeFrac(renderInfo.TickFraction);
        program.LightLevelMix(renderInfo.Uniforms.Mix);
        program.ExtraLight(renderInfo.Uniforms.ExtraLightOrColorMapIndex);
        program.DistanceOffset(renderInfo.Uniforms.DistanceOffset);
        program.ColorMix(renderInfo.Uniforms.ColorMix.Global);
        program.PaletteIndex((int)renderInfo.Uniforms.PaletteIndex);
        program.ColorMapIndex(renderInfo.Uniforms.ColorMapUniforms.GlobalIndex);
        program.LightMode(renderInfo.Uniforms.LightMode);
        program.GammaCorrection(renderInfo.Uniforms.GammaCorrection);
        program.UseBrightmaps(renderInfo.Uniforms.UseBrightmaps);
        program.SetSpriteClipDownScaleAmount(renderInfo.Uniforms.DownScaleAmount);
        program.ScreenBounds((renderInfo.Viewport.Width - 1, renderInfo.Viewport.Height - 1));
        program.CheckPlaneClip(false);
        program.FogBarrier(false);

        if (program is InterpolationCompositeShader)
        {
            program.AccumTexture(BindTextures.AccumTexture);
            program.AccumCountTextre(BindTextures.AccumCountTexture);
        }

        if (program is InterpolationTransparentShader || program is InterpolationCompositeShader)
            program.CheckPlaneClip(m_vanillaRender);
    }

    private void SetStaticUniforms(StaticShader program, RenderInfo renderInfo)
    {
        program.BoundTexture(BindTextures.BoundTexture);
        program.SectorLightTexture(BindTextures.SectorLight);
        program.ColormapTexture(BindTextures.Colormap);
        program.SectorColormapTexture(BindTextures.SectorColormap);
        program.SectorFogTexture(BindTextures.SectorFog);
        program.BrightmapTexture(BindTextures.BrightmapTexture);
        program.PlaneClipTexture(BindTextures.PlaneClipTexture);
        program.WallClipTexture(BindTextures.WallClipTexture);
        program.HasInvulnerability(renderInfo.Uniforms.DrawInvulnerability);
        program.Mvp(renderInfo.Uniforms.Mvp);
        program.MvpNoPitch(renderInfo.Uniforms.MvpNoPitch);
        program.LightLevelMix(renderInfo.Uniforms.Mix);
        program.ExtraLight(renderInfo.Uniforms.ExtraLightOrColorMapIndex);
        program.DistanceOffset(renderInfo.Uniforms.DistanceOffset);
        program.ColorMix(renderInfo.Uniforms.ColorMix.Global);
        program.PaletteIndex((int)renderInfo.Uniforms.PaletteIndex);
        program.ColorMapIndex(renderInfo.Uniforms.ColorMapUniforms.GlobalIndex);
        program.LightMode(renderInfo.Uniforms.LightMode);
        program.GammaCorrection(renderInfo.Uniforms.GammaCorrection);
        program.UseBrightmaps(renderInfo.Uniforms.UseBrightmaps);
        program.SetSpriteClipDownScaleAmount(renderInfo.Uniforms.DownScaleAmount);
        program.ScreenBounds((renderInfo.Viewport.Width - 1, renderInfo.Viewport.Height - 1));
        program.CheckPlaneClip(false);
        program.FogBarrier(false);

        if (program is StaticCompositeShader)
        {
            program.AccumTexture(BindTextures.AccumTexture);
            program.AccumCountTexture(BindTextures.AccumCountTexture);            
        }

        if (program is StaticCompositeShader || program is StaticTransparentShader)
            program.CheckPlaneClip(m_vanillaRender);
    }

    private void ReleaseUnmanagedResources()
    {
        m_interpolationProgram.Dispose();
        m_staticProgram.Dispose();
        m_geometryRenderer.Dispose();
        m_worldDataManager.Dispose();
        m_primitiveRenderer.Dispose();
        m_entityRenderer.Dispose();
    }
}
