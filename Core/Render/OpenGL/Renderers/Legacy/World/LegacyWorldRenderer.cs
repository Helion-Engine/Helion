using System;
using System.Diagnostics;
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
    private readonly InterpolationPlaneClipShader m_interpolationPlaneClipShader = new();
    private readonly InterpolationWallClipShader m_interpolationWallClipShader = new();
    private readonly StaticShader m_staticProgram = new("Main");
    private readonly StaticPlaneClipShader m_staticPlaneClipProgram = new();
    private readonly StaticWallClipShader m_staticWallClipProgram = new();
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

                for (var entity = world.RenderBlockmap.HeadRenderEntities[index]; entity != null; entity = entity.RenderBlockNext)
                    RenderEntity(world, entity);
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
            var sectorIsland = islandNode.Value;
            if (sectorIsland.BlockmapCount == m_renderData.CheckCount)
                continue;

            sectorIsland.BlockmapCount = m_renderData.CheckCount;
            if (sectorIsland.ParentIsland != null && sectorIsland.ParentIsland != m_renderData.ViewIsland)
                continue;

            var sector = world.Sectors[sectorIsland.SectorId];
            if (sector.CheckCount == m_renderData.CheckCount)
                continue;

            double dx1 = Math.Max(sectorIsland.Box.Min.X - m_renderData.ViewPosInterpolated.X, Math.Max(0, m_renderData.ViewPosInterpolated.X - sectorIsland.Box.Max.X));
            double dy1 = Math.Max(sectorIsland.Box.Min.Y - m_renderData.ViewPosInterpolated.Y, Math.Max(0, m_renderData.ViewPosInterpolated.Y - sectorIsland.Box.Max.Y));
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

        // DynamicSides are either scrolling textures or alpha, neither should setup cover walls.
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

    void RenderEntity(IWorld world, Entity entity)
    {
        if (entity.FrameState.Frame.IsInvisible || entity.Flags.Invisible || entity.Flags.NoSector || entity == m_viewerEntity || entity.Properties.RenderStyle == RenderStyle.None)
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
        m_entityRenderer.RenderEntity(entity, m_renderData.ViewPosInterpolated);     
    }

    protected override void PerformRender(IWorld world, RenderInfo renderInfo, GLFramebuffer framebuffer)
    {
        // If the transfer height view is not the middle then the cached static geometry cannot be used.
        // Render all sectors dynamically instead.
        m_lastRenderStatic = m_renderStatic;
        m_renderStatic = renderInfo.TransferHeightView == TransferHeightView.Middle;
        Clear(world, renderInfo);

        if (framebuffer.DepthTexture == null)
            throw new Exception("Framebuffer must have a depth texture.");

        var dimension = new Dimension(renderInfo.Viewport.Width, renderInfo.Viewport.Height);
        m_oitFrameBuffer.CreateOrUpdate(dimension, framebuffer.DepthTexture);

        SetupClipBuffers(framebuffer, dimension);

        if (m_lastTicker != world.GameTicker)
            m_entityRenderer.Start(renderInfo);

        SetOccludePosition(renderInfo.Camera.PositionInterpolated.Double, renderInfo.Camera.YawRadians, renderInfo.Camera.PitchRadians,
            ref m_occlude, ref m_occludeViewPos);
        IterateBlockmap(world, renderInfo);
        PopulatePrimitives(world);

        m_geometryRenderer.RenderSkies(renderInfo);
        m_geometryRenderer.RenderPortals(renderInfo);

        if (m_renderStatic)
            m_geometryRenderer.RenderStaticSkies(renderInfo);

        m_primitiveRenderer.Render(renderInfo);

        if (!m_vanillaRender)
        {
            m_interpolationProgram.Bind();
            GL.ActiveTexture(BindTextures.BoundTexture);
            SetInterpolationUniforms(m_interpolationProgram, renderInfo, false);
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
        SetInterpolationUniforms(m_interpolationProgram, renderInfo, false);
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

        GL.Clear(ClearBufferMask.DepthBufferBit);
        GL.ColorMask(false, false, false, false);
        // Write two-sided middle walls to depth as these generally look better with normal discard handling
        RenderTwoSidedMiddleWalls(renderInfo);
        GL.ColorMask(true, true, true, true);

        if (m_wallClipFrameBuffer != null || m_planeClipFrameBuffer != null)
            WriteSpriteClipBuffers(renderInfo, framebuffer);

        m_entityRenderer.RenderOpaque(renderInfo);
        RenderTransparent(renderInfo, framebuffer);
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

        if (m_wallClipFrameBuffer != null)
            WritePlaneClipData(m_wallClipFrameBuffer, useRenderInfo, framebuffer, true);

        if (m_planeClipFrameBuffer != null)
            WritePlaneClipData(m_planeClipFrameBuffer, useRenderInfo, framebuffer, false);

        if (renderInfo.Uniforms.DownScaleAmount > 1)
            GL.Viewport(renderInfo.Viewport.X, renderInfo.Viewport.Y, renderInfo.Viewport.Width, renderInfo.Viewport.Height);
    }

    private void SetupClipBuffers(GLFramebuffer framebuffer, Dimension dimension)
    {
        bool bind = false;
        if (m_planeClipFrameBuffer != null)
        {
            m_planeClipFrameBuffer.CreateOrUpdate("PlaneClip", dimension);
            m_planeClipFrameBuffer.BindFrameBuffer();
            m_planeClipFrameBuffer.Clear();
            bind = true;
        }

        if (m_wallClipFrameBuffer != null)
        {
            m_wallClipFrameBuffer.CreateOrUpdate("WallClip", dimension);
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

        if (m_renderStatic)
        {
            if (walls)
            {
                m_staticWallClipProgram.Bind();
                GL.ActiveTexture(BindTextures.BoundTexture);
                SetStaticUniforms(m_staticWallClipProgram, renderInfo);
                m_geometryRenderer.RenderStaticOneSidedCoverWalls();
                GL.Disable(EnableCap.CullFace);
                m_geometryRenderer.RenderStaticCoverWalls();
                GL.Enable(EnableCap.CullFace);
                m_staticWallClipProgram.Unbind();
            }
            else
            {
                m_staticPlaneClipProgram.Bind();
                GL.ActiveTexture(BindTextures.BoundTexture);
                SetStaticUniforms(m_staticPlaneClipProgram, renderInfo);
                m_geometryRenderer.RenderStaticGeometryFlats();
                m_staticPlaneClipProgram.Unbind();
            }
        }

        if (walls)
        {
            m_interpolationWallClipShader.Bind();
            GL.ActiveTexture(BindTextures.BoundTexture);
            SetInterpolationUniforms(m_interpolationWallClipShader, renderInfo, false);
            GL.Disable(EnableCap.CullFace);
            m_worldDataManager.RenderCoverWalls();
            m_geometryRenderer.RenderWallClipPortals(renderInfo);
            GL.Enable(EnableCap.CullFace);
            m_interpolationWallClipShader.Unbind();
        }
        else
        {
            m_interpolationPlaneClipShader.Bind();
            GL.ActiveTexture(BindTextures.BoundTexture);
            SetInterpolationUniforms(m_interpolationPlaneClipShader, renderInfo, false);
            m_worldDataManager.RenderFlats();
            m_interpolationPlaneClipShader.Unbind();
        }

        PlaneClipFrameBuffer.UnbindFrameBuffer();
        framebuffer.Bind();
        planeClipFrameBuffer.BindPlaneTexture(walls ? BindTextures.WallClipTexture : BindTextures.PlaneClipTexture);
        ResetBlendEquations();
    }

    private unsafe void RenderTransparent(RenderInfo renderInfo, GLFramebuffer framebuffer)
    {
        var fuzzData = m_entityRenderer.HasDataToRenderByStyle(RenderDataStyle.Fuzzy); 
        var alphaData = m_entityRenderer.HasDataToRenderByStyle(RenderDataStyle.Translucent) || m_entityRenderer.HasDataToRenderByStyle(RenderDataStyle.Add) || 
            m_entityRenderer.HasDataToRenderByStyle(RenderDataStyle.ColorAdd);
        var alphaWalls = m_worldDataManager.HasAlphaWalls();
        if (!fuzzData && !alphaData && !alphaWalls)
            return;

        m_oitFrameBuffer.StartRender();
        GL.DepthMask(false);
        m_entityRenderer.RenderOitTransparentPass(renderInfo);

        m_oitFrameBuffer.BindTextures(BindTextures.AccumTexture, BindTextures.AccumCountTexture, BindTextures.FuzzTexture, BindTextures.OpaqueTexture, framebuffer);

        if (fuzzData)
        {
            if (GLInfo.MemoryBarrierSupported)
                GL.MemoryBarrier(MemoryBarrierFlags.FramebufferBarrierBit);

            ResetBlendEquations();
            framebuffer.Bind();
            // Refract pixels in the opaque framebuffer
            m_entityRenderer.RenderOitFuzzRefractionPass(renderInfo, false);

            OitFrameBuffer.SetBlendEquations();
            m_oitFrameBuffer.BindFrameBuffer();

            m_entityRenderer.RenderOitTransparentFuzzPass(renderInfo);
        }

        m_interpolationTransparentProgram.Bind();
        // Alpha walls are the exception to pixel gap correction. Only required for opaque walls.
        m_interpolationTransparentProgram.VertexGapClampUV(false);
        SetInterpolationUniforms(m_interpolationTransparentProgram, renderInfo, m_vanillaRender);
        GL.ActiveTexture(BindTextures.BoundTexture);
        m_worldDataManager.RenderAlphaWalls();

        ResetBlendEquations();
        framebuffer.Bind();

        m_entityRenderer.RenderOitCompositePass(renderInfo);

        if (alphaWalls)
        {
            m_interpolationCompositeProgram.Bind();
            m_interpolationCompositeProgram.VertexGapClampUV(false);
            SetInterpolationUniforms(m_interpolationCompositeProgram, renderInfo, m_vanillaRender);
            GL.ActiveTexture(BindTextures.BoundTexture);
            m_worldDataManager.RenderAlphaWalls();
        }

        if (fuzzData)
            m_entityRenderer.RenderOitFuzzRefractionPass(renderInfo, true);

        OitFrameBuffer.UnbindFrameBuffer();
        GL.DepthMask(true);
    }

    private static void ResetBlendEquations()
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

        if (m_renderStatic)
        {
            m_staticProgram.Bind();
            GL.ActiveTexture(BindTextures.BoundTexture);
            SetStaticUniforms(m_staticProgram, renderInfo);
            m_staticProgram.VertexGapClampUV(m_pixelGapCorrection);
            m_geometryRenderer.RenderStaticTwoSidedWalls();
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

    private static void SetInterpolationUniforms(InterpolationShader program, RenderInfo renderInfo, bool checkPlaneClip)
    {
        program.Bind();
        program.BoundTexture(BindTextures.BoundTexture);
        program.SectorLightTexture(BindTextures.SectorLight);
        program.ColormapTexture(BindTextures.Colormap);
        program.SectorColormapTexture(BindTextures.SectorColormap);
        program.BrightmapTexture(BindTextures.BrightmapTexture);
        program.PlaneClipTexture(BindTextures.PlaneClipTexture);
        program.WallClipTexture(BindTextures.WallClipTexture);
        program.HasInvulnerability(renderInfo.Uniforms.DrawInvulnerability);
        program.Mvp(renderInfo.Uniforms.Mvp);
        program.MvpNoPitch(renderInfo.Uniforms.MvpNoPitch);
        program.TimeFrac(renderInfo.TickFraction);
        program.LightLevelMix(renderInfo.Uniforms.Mix);
        program.ExtraLight(renderInfo.Uniforms.ExtraLight);
        program.DistanceOffset(renderInfo.Uniforms.DistanceOffset);
        program.ColorMix(renderInfo.Uniforms.ColorMix.Global);
        program.PaletteIndex((int)renderInfo.Uniforms.PaletteIndex);
        program.ColorMapIndex(renderInfo.Uniforms.ColorMapUniforms.GlobalIndex);
        program.LightMode(renderInfo.Uniforms.LightMode);
        program.GammaCorrection(renderInfo.Uniforms.GammaCorrection);
        program.CheckPlaneClip(checkPlaneClip);
        program.UseBrightmaps(renderInfo.Uniforms.UseBrightmaps);
        program.SetSpriteClipDownScaleAmount(renderInfo.Uniforms.DownScaleAmount);
        program.ScreenBounds((renderInfo.Viewport.Width - 1, renderInfo.Viewport.Height - 1));

        if (program is InterpolationCompositeShader)
        {
            program.AccumTexture(BindTextures.AccumTexture);
            program.AccumCountTextre(BindTextures.AccumCountTexture);
        }
    }

    private static void SetStaticUniforms(StaticShader program, RenderInfo renderInfo)
    {
        program.BoundTexture(BindTextures.BoundTexture);
        program.SectorLightTexture(BindTextures.SectorLight);
        program.ColormapTexture(BindTextures.Colormap);
        program.SectorColormapTexture(BindTextures.SectorColormap);
        program.BrightmapTexture(BindTextures.BrightmapTexture);
        program.HasInvulnerability(renderInfo.Uniforms.DrawInvulnerability);
        program.Mvp(renderInfo.Uniforms.Mvp);
        program.MvpNoPitch(renderInfo.Uniforms.MvpNoPitch);
        program.LightLevelMix(renderInfo.Uniforms.Mix);
        program.ExtraLight(renderInfo.Uniforms.ExtraLight);
        program.DistanceOffset(renderInfo.Uniforms.DistanceOffset);
        program.ColorMix(renderInfo.Uniforms.ColorMix.Global);
        program.PaletteIndex((int)renderInfo.Uniforms.PaletteIndex);
        program.ColorMapIndex(renderInfo.Uniforms.ColorMapUniforms.GlobalIndex);
        program.LightMode(renderInfo.Uniforms.LightMode);
        program.GammaCorrection(renderInfo.Uniforms.GammaCorrection);
        program.UseBrightmaps(renderInfo.Uniforms.UseBrightmaps);
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
