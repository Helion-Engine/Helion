using System;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Graphics.Palettes;
using Helion.Render.OpenGL.Renderers.Legacy.World.Automap;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Renderers.Legacy.World.Entities;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Renderers.Legacy.World.Primitives;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Impl;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Blockmap;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Static;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class LegacyWorldRenderer : WorldRenderer
{
    private readonly IConfig m_config;
    private readonly GeometryRenderer m_geometryRenderer;
    private readonly EntityRenderer m_entityRenderer;
    private readonly PrimitiveWorldRenderer m_primitiveRenderer;
    private readonly InterpolationShader m_interpolationProgram = new();
    private readonly StaticShader m_staticProgram = new();
    private readonly RenderWorldDataManager m_worldDataManager = new();
    private readonly DynamicArray<IRenderObject> m_alphaEntities = new(256);
    private readonly Comparison<IRenderObject> m_renderObjectComparer = new(RenderObjectCompare);
    private readonly ArchiveCollection m_archiveCollection;
    private readonly LegacyGLTextureManager m_textureManager;
    private Sector m_viewSector;
    private Vec2D m_occludeViewPos;
    private bool m_occlude;
    private bool m_spriteTransparency;
    private bool m_vanillaRender;
    private int m_lastTicker = -1;
    private int m_renderCount;
    private int m_maxDistance;
    private Entity? m_viewerEntity;
    private IWorld? m_previousWorld;
    private RenderBlockMapData m_renderData;

    public LegacyWorldRenderer(IConfig config, ArchiveCollection archiveCollection, LegacyGLTextureManager textureManager)
    {
        m_config = config;
        m_entityRenderer = new(config, textureManager);
        m_primitiveRenderer = new();
        m_viewSector = Sector.CreateDefault();
        m_geometryRenderer = new(config, archiveCollection, textureManager, m_interpolationProgram, m_staticProgram, m_worldDataManager);
        m_archiveCollection = archiveCollection;
        m_textureManager = textureManager;
        m_maxDistance = config.Render.MaxDistance;
    }

    static int RenderObjectCompare(IRenderObject? x, IRenderObject? y)
    {
        if (x == null || y == null)
            return 1;

        // Reverse distance order
        return y.RenderDistanceSquared.CompareTo(x.RenderDistanceSquared);
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
        m_vanillaRender = m_config.Render.VanillaRender;
        TransferHeights.FlushSectorReferences();
        m_lastRenderedWorld.SetTarget(world);

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
        m_alphaEntities.FlushReferences();
    }

    private void World_OnResetInterpolation(object? sender, EventArgs e)
    {
        m_lastTicker = -1;
        ResetInterpolation((IWorld)sender!);
    }

    private void IterateBlockmap(IWorld world, RenderInfo renderInfo)
    {
        bool shouldRender = m_lastTicker != world.GameTicker;
        if (!shouldRender)
            return;

        m_renderCount = ++WorldStatic.CheckCounter;
        m_renderData.ViewerEntity = renderInfo.ViewerEntity;
        m_renderData.ViewPosInterpolated = renderInfo.Camera.PositionInterpolated.XY.Double;
        m_renderData.ViewPosInterpolated3D = renderInfo.Camera.PositionInterpolated.Double;
        m_renderData.ViewPos3D = renderInfo.Camera.Position.Double;
        m_renderData.ViewDirection = renderInfo.Camera.Direction.XY.Double;
        m_renderData.ViewIsland = world.Geometry.IslandGeometry.Islands[world.Geometry.BspTree.Subsectors[renderInfo.ViewerEntity.Subsector.Id].IslandId];
        m_viewSector = renderInfo.ViewSector;

        m_viewerEntity = renderInfo.ViewerEntity;
        m_geometryRenderer.Clear(renderInfo.TickFraction, true);
        m_renderData.CheckCount = ++WorldStatic.CheckCounter;

        m_renderData.MaxDistance = world.Config.Render.MaxDistance;
        if (m_renderData.MaxDistance <= 0)
            m_renderData.MaxDistance = 6000;

        m_renderData.MaxDistanceSquared = m_renderData.MaxDistance * m_renderData.MaxDistance;
        m_renderData.OccludePos = m_occlude ? m_occludeViewPos : null;
        Box2D box = new(m_renderData.ViewPosInterpolated.X, m_renderData.ViewPosInterpolated.Y, m_renderData.MaxDistance);

        Vec2D occluder = m_renderData.OccludePos ?? Vec2D.Zero;
        bool occlude = m_renderData.OccludePos.HasValue;

        var renderBlocks = world.RenderBlockmap.Blocks;
        var it = renderBlocks.CreateBoxIteration(box);
        for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
        {
            for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
            {
                Block block = renderBlocks[by * it.Width + bx];
                if (occlude && !block.Box.InView(occluder, m_renderData.ViewDirection))
                    continue;

                for (var islandNode = block.DynamicSectors.Head; islandNode != null; islandNode = islandNode.Next)
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

                    var transfer = sector.TransferHeights;
                    const SectorDynamic MovementFlags = SectorDynamic.Movement | SectorDynamic.Scroll;
                    // Middle view is in the static renderer. If it's not moving then we don't need to dynamically draw.
                    if (transfer != null && renderInfo.TransferHeightView == TransferHeightView.Middle &&
                        (sector.Floor.Dynamic & MovementFlags) == 0 &&
                        (sector.Ceiling.Dynamic & MovementFlags) == 0 &&
                        (transfer.ControlSector.Floor.Dynamic & MovementFlags) == 0 &&
                        (transfer.ControlSector.Ceiling.Dynamic & MovementFlags) == 0)
                        continue;

                    double dx1 = Math.Max(sectorIsland.Box.Min.X - m_renderData.ViewPosInterpolated.X, Math.Max(0, m_renderData.ViewPosInterpolated.X - sectorIsland.Box.Max.X));
                    double dy1 = Math.Max(sectorIsland.Box.Min.Y - m_renderData.ViewPosInterpolated.Y, Math.Max(0, m_renderData.ViewPosInterpolated.Y - sectorIsland.Box.Max.Y));
                    if (dx1 * dx1 + dy1 * dy1 <= m_renderData.MaxDistanceSquared)
                    {                        
                        m_geometryRenderer.RenderSector(m_viewSector, sector, m_renderData.ViewPos3D, m_renderData.ViewPosInterpolated3D);
                        sector.CheckCount = m_renderData.CheckCount;
                    }
                }

                // DynamicSides are either scrolling textures or alpha, neither should setup cover walls.
                m_geometryRenderer.SetBufferCoverWall(false);
                for (int i = 0; i < block.DynamicSides.Length; i++)
                {
                    var side = block.DynamicSides.Data[i];
                    if (side.BlockmapCount == m_renderData.CheckCount)
                        continue;
                    if (side.Sector.IsMoving || (side.PartnerSide != null && side.PartnerSide.Sector.IsMoving))
                        continue;

                    side.BlockmapCount = m_renderData.CheckCount;
                    m_geometryRenderer.RenderSectorWall(m_viewSector, side.Sector, side.Line,
                        m_renderData.ViewPos3D, m_renderData.ViewPosInterpolated3D);
                }
                m_geometryRenderer.SetBufferCoverWall(true);

                for (var entity = block.HeadEntity; entity != null; entity = entity.RenderBlockNext)
                    RenderEntity(world, entity);
            }
        }

        m_lastTicker = world.GameTicker;

        RenderAlphaObjects(m_alphaEntities);
        m_alphaEntities.Clear();
    }

    void RenderEntity(IWorld world, Entity entity)
    {
        if (entity.Frame.IsInvisible || entity.Flags.Invisible || entity.Flags.NoSector || entity == m_viewerEntity)
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
        if ((m_spriteTransparency && entity.Alpha < 1) || entity.Definition.Flags.Shadow)
        {
            m_alphaEntities.Add(entity);
            return;
        }

        m_entityRenderer.RenderEntity(entity, m_renderData.ViewPosInterpolated);     
    }

    protected override void PerformRender(IWorld world, RenderInfo renderInfo)
    {
        m_spriteTransparency = m_config.Render.SpriteTransparency;
        m_maxDistance = m_config.Render.MaxDistance;
        Clear(world, renderInfo);

        if (m_lastTicker != world.GameTicker)
            m_entityRenderer.Start(renderInfo);
        SetOccludePosition(renderInfo.Camera.PositionInterpolated.Double, renderInfo.Camera.YawRadians, renderInfo.Camera.PitchRadians,
            ref m_occlude, ref m_occludeViewPos);
        IterateBlockmap(world, renderInfo);
        PopulatePrimitives(world);

        m_geometryRenderer.RenderPortalsAndSkies(renderInfo);

        if (!m_vanillaRender)
        {
            m_interpolationProgram.Bind();
            GL.ActiveTexture(TextureUnit.Texture0);
            SetInterpolationUniforms(renderInfo);
            m_worldDataManager.RenderWalls();
            m_worldDataManager.RenderTwoSidedMiddleWalls();
            m_worldDataManager.RenderFlats();

            m_staticProgram.Bind();
            GL.ActiveTexture(TextureUnit.Texture0);
            SetStaticUniforms(renderInfo);
            m_geometryRenderer.StartRenderStaticGeometry();
            m_geometryRenderer.RenderStaticGeometryWalls();
            m_geometryRenderer.RenderStaticTwoSidedWalls();
            m_geometryRenderer.RenderStaticGeometryFlats();
            m_entityRenderer.RenderNonAlpha(renderInfo);
            m_entityRenderer.RenderAlpha(renderInfo);

            m_interpolationProgram.Bind();
            GL.ActiveTexture(TextureUnit.Texture0);
            m_worldDataManager.RenderAlphaWalls();
            m_interpolationProgram.Unbind();

            m_primitiveRenderer.Render(renderInfo);
            return;
        }

        m_interpolationProgram.Bind();
        GL.ActiveTexture(TextureUnit.Texture0);
        SetInterpolationUniforms(renderInfo);
        m_worldDataManager.RenderWalls();
        m_worldDataManager.RenderFlats();
        m_worldDataManager.RenderTwoSidedMiddleWalls();

        m_staticProgram.Bind();
        GL.ActiveTexture(TextureUnit.Texture0);
        SetStaticUniforms(renderInfo);
        m_geometryRenderer.StartRenderStaticGeometry();
        m_geometryRenderer.RenderStaticGeometryWalls();
        m_geometryRenderer.RenderStaticGeometryFlats();
        m_geometryRenderer.RenderStaticTwoSidedWalls();

        GL.Clear(ClearBufferMask.DepthBufferBit);
        GL.ColorMask(false, false, false, false);
        GL.Disable(EnableCap.CullFace);
        m_staticProgram.Bind();
        GL.ActiveTexture(TextureUnit.Texture0);
        m_geometryRenderer.RenderStaticCoverWalls();
        m_interpolationProgram.Bind();
        m_worldDataManager.RenderCoverWalls();
        // Need to render flood fill again. Sprites need to be blocked by flood filling if visible.
        m_geometryRenderer.Portals.Render(renderInfo);
        GL.Enable(EnableCap.CullFace);
        m_staticProgram.Bind();
        GL.ActiveTexture(TextureUnit.Texture0);
        m_geometryRenderer.RenderStaticOneSidedCoverWalls();
        m_geometryRenderer.RenderStaticTwoSidedWalls();
        m_interpolationProgram.Bind();
        m_worldDataManager.RenderTwoSidedMiddleWalls();
        GL.ColorMask(true, true, true, true);

        m_entityRenderer.RenderNonAlpha(renderInfo);
        m_entityRenderer.RenderAlpha(renderInfo);

        m_interpolationProgram.Bind();
        GL.ActiveTexture(TextureUnit.Texture0);
        m_worldDataManager.RenderAlphaWalls();
        m_interpolationProgram.Unbind();

        m_primitiveRenderer.Render(renderInfo);
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

    private void RenderAlphaObjects(DynamicArray<IRenderObject> alphaEntities)
    {
        // This will just render based on distance from their center point.
        // Not really correct, but mostly correct enough for now.
        DynamicArray<IRenderObject> alphaObjects = alphaEntities;
        alphaObjects.AddRange(m_geometryRenderer.AlphaSides);
        alphaObjects.Sort(m_renderObjectComparer);

        Vec2D prevPos = m_renderData.ViewPosInterpolated;

        for (int i = 0; i < alphaObjects.Length; i++)
        {
            IRenderObject renderObject = alphaObjects[i];
            if (renderObject.Type == RenderObjectType.Entity)
            {
                Entity entity = (Entity)renderObject;
                m_entityRenderer.RenderEntity(entity, m_renderData.ViewPosInterpolated);
            }
            else if (renderObject.Type == RenderObjectType.Side)
            {
                Side side = (Side)renderObject;
                bool onFront = side.Line.Segment.OnRight(prevPos);
                if (side.IsFront == onFront)
                    m_geometryRenderer.RenderAlphaSide(side, onFront);
            }
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

    private void SetInterpolationUniforms(RenderInfo renderInfo)
    {
        m_interpolationProgram.BoundTexture(TextureUnit.Texture0);
        m_interpolationProgram.SectorLightTexture(TextureUnit.Texture1);
        m_interpolationProgram.HasInvulnerability(renderInfo.Uniforms.DrawInvulnerability);
        m_interpolationProgram.Mvp(renderInfo.Uniforms.Mvp);
        m_interpolationProgram.MvpNoPitch(renderInfo.Uniforms.MvpNoPitch);
        m_interpolationProgram.TimeFrac(renderInfo.TickFraction);
        m_interpolationProgram.LightLevelMix(renderInfo.Uniforms.Mix);
        m_interpolationProgram.ExtraLight(renderInfo.Uniforms.ExtraLight);
        m_interpolationProgram.DistanceOffset(renderInfo.Uniforms.DistanceOffset);
        m_interpolationProgram.ColorMix(renderInfo.Uniforms.ColorMix);
    }

    private void SetStaticUniforms(RenderInfo renderInfo)
    {
        m_staticProgram.BoundTexture(TextureUnit.Texture0);
        m_staticProgram.SectorLightTexture(TextureUnit.Texture1);
        m_staticProgram.HasInvulnerability(renderInfo.Uniforms.DrawInvulnerability);
        m_staticProgram.Mvp(renderInfo.Uniforms.Mvp);
        m_staticProgram.MvpNoPitch(renderInfo.Uniforms.MvpNoPitch);
        m_staticProgram.LightLevelMix(renderInfo.Uniforms.Mix);
        m_staticProgram.ExtraLight(renderInfo.Uniforms.ExtraLight);
        m_staticProgram.DistanceOffset(renderInfo.Uniforms.DistanceOffset);
        m_staticProgram.ColorMix(renderInfo.Uniforms.ColorMix);
    }

    private void ReleaseUnmanagedResources()
    {
        m_interpolationProgram.Dispose();
        m_staticProgram.Dispose();
        m_geometryRenderer.Dispose();
        m_worldDataManager.Dispose();
        //m_automapRenderer.Dispose();
        m_primitiveRenderer.Dispose();
        m_entityRenderer.Dispose();
    }
}
