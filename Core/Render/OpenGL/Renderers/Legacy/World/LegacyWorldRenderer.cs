using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Grids;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Automap;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Renderers.Legacy.World.Entities;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Renderers.Legacy.World.Primitives;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World.ViewClipping;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Blockmap;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Helion.World.Static;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class LegacyWorldRenderer : WorldRenderer
{
    struct RenderBlockMapData
    {
        public Vec2D? OccludePos;
        public Vec2D ViewPos;
        public Vec2D ViewDirection;
        public Vec3D ViewPos3D;
        public int CheckCount;
        public int MaxDistance;
    }

    private readonly IConfig m_config;
    private readonly GeometryRenderer m_geometryRenderer;
    private readonly EntityRenderer m_entityRenderer;
    private readonly PrimitiveWorldRenderer m_primitiveRenderer;
    private readonly LegacyShader m_program = new();
    private readonly RenderWorldDataManager m_worldDataManager = new();
    private readonly LegacyAutomapRenderer m_automapRenderer;
    private readonly ViewClipper m_viewClipper;
    private readonly DynamicArray<IRenderObject> m_alphaEntities = new();
    private readonly RenderObjectComparer m_renderObjectComparer = new();
    private readonly ArchiveCollection m_archiveCollection;    
    private Sector m_viewSector;
    private Vec2D m_occludeViewPos;
    private bool m_occlude;
    private bool m_spriteTransparency;
    private int m_lastTicker = -1;
    private int m_renderCount;
    private IWorld? m_previousWorld;
    private RenderBlockMapData m_renderData;

    public IWorld? World => m_previousWorld;

    public LegacyWorldRenderer(IConfig config, ArchiveCollection archiveCollection, LegacyGLTextureManager textureManager)
    {
        m_config = config;
        m_automapRenderer = new(archiveCollection);
        m_entityRenderer = new(config, textureManager, m_worldDataManager);
        m_primitiveRenderer = new();
        m_viewClipper = new(archiveCollection.DataCache);
        m_viewSector = Sector.CreateDefault();
        m_geometryRenderer = new(config, archiveCollection, textureManager, m_program, m_viewClipper, m_worldDataManager);
        m_archiveCollection = archiveCollection;
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

    protected override void UpdateToNewWorld(IWorld world)
    {
        if (m_previousWorld != null)
            m_previousWorld.OnResetInterpolation -= World_OnResetInterpolation;

        m_geometryRenderer.UpdateTo(world);
        world.OnResetInterpolation += World_OnResetInterpolation;
        m_previousWorld = world;
        m_lastTicker = -1;
        m_alphaEntities.FlushReferences();
    }

    private void World_OnResetInterpolation(object? sender, EventArgs e)
    {
        m_lastTicker = -1;
        ResetInterpolation((IWorld)sender);
    }

    protected override void PerformAutomapRender(IWorld world, RenderInfo renderInfo)
    {
        Clear(world, renderInfo);
        if (!m_config.Render.Blockmap)
            TraverseBsp(world, renderInfo);

        m_automapRenderer.Render(world, renderInfo);
    }

    private void IterateBlockmap(IWorld world, RenderInfo renderInfo)
    {
        bool shouldRender = m_lastTicker != world.GameTicker;
        if (!shouldRender)
            return;

        m_renderData.ViewPos = renderInfo.Camera.Position.XY.Double;
        m_renderData.ViewPos3D = renderInfo.Camera.Position.Double;
        m_renderData.ViewDirection = renderInfo.Camera.Direction.XY.Double;
        m_viewSector = world.BspTree.ToSector(m_renderData.ViewPos3D);

        TransferHeightView transferHeightsView = TransferHeights.GetView(m_viewSector, renderInfo.Camera.Position.Z);

        m_geometryRenderer.Clear(renderInfo.TickFraction, true);
        m_entityRenderer.SetViewDirection(renderInfo.ViewerEntity, m_renderData.ViewDirection);
        m_entityRenderer.SetTickFraction(renderInfo.TickFraction);
        m_renderData.CheckCount = ++world.CheckCounter;

        m_renderData.MaxDistance = world.Config.Render.MaxDistance;
        if (m_renderData.MaxDistance <= 0)
            m_renderData.MaxDistance = 6000;

        m_renderData.OccludePos = m_occlude ? m_occludeViewPos : null;
        Box2D box = new(m_renderData.ViewPos, m_renderData.MaxDistance);

        double maxDistSquared = m_renderData.MaxDistance * m_renderData.MaxDistance;
        Vec2D occluder = m_renderData.OccludePos ?? Vec2D.Zero;
        bool occlude = m_renderData.OccludePos.HasValue;

        BlockmapBoxIterator<Block> it = world.RenderBlockmap.Iterate(box);
        while (it.HasNext())
        {
            Block block = it.Next();

            if (occlude && !block.Box.InView(occluder, m_renderData.ViewDirection))
                continue;

            for (LinkableNode<Sector>? sectorNode = block.DynamicSectors.Head; sectorNode != null; sectorNode = sectorNode.Next)
            {
                if (sectorNode.Value.BlockmapCount == m_renderData.CheckCount)
                    continue;

                Sector sector = sectorNode.Value;
                TransferHeights? transfer = sector.TransferHeights;
                sector.BlockmapCount = m_renderData.CheckCount;

                // Middle view is in the static renderer. If it's not moving then we don't need to dynamically draw.
                if (transfer != null && transferHeightsView == TransferHeightView.Middle &&
                    (sector.Floor.Dynamic & SectorDynamic.Movement) == 0 &&
                    (sector.Ceiling.Dynamic & SectorDynamic.Movement) == 0 &&
                    (transfer.ControlSector.Floor.Dynamic & SectorDynamic.Movement) == 0 && 
                    (transfer.ControlSector.Ceiling.Dynamic & SectorDynamic.Movement) == 0)
                    continue;

                Box2D sectorBox = sector.GetBoundingBox();
                double dx1 = Math.Max(sectorBox.Min.X - m_renderData.ViewPos.X, Math.Max(0, m_renderData.ViewPos.X - sectorBox.Max.X));
                double dy1 = Math.Max(sectorBox.Min.Y - m_renderData.ViewPos.Y, Math.Max(0, m_renderData.ViewPos.Y - sectorBox.Max.Y));
                if (dx1 * dx1 + dy1 * dy1 <= maxDistSquared)
                    RenderSector(sector);
            }

            for (LinkableNode<Side>? sideNode = block.DynamicSides.Head; sideNode != null; sideNode = sideNode.Next)
            {
                if (sideNode.Value.BlockmapCount == m_renderData.CheckCount)
                    continue;
                if (sideNode.Value.Sector.IsMoving || (sideNode.Value.PartnerSide != null && sideNode.Value.PartnerSide.Sector.IsMoving))
                    continue;

                sideNode.Value.BlockmapCount = m_renderData.CheckCount;
                m_geometryRenderer.RenderSectorWall(m_viewSector, sideNode.Value.Sector, sideNode.Value.Line, m_renderData.ViewPos3D);
            }

            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                if (entityNode.Value.BlockmapCount == m_renderData.CheckCount)
                    continue;

                entityNode.Value.BlockmapCount = m_renderData.CheckCount;
                RenderEntity(entityNode.Value);
            }
        }

        m_lastTicker = world.GameTicker;

        RenderAlphaObjects(m_renderData.ViewPos, m_renderData.ViewPos3D, m_alphaEntities);
        m_alphaEntities.Clear();
    }

    void RenderEntity(Entity entity)
    {
        if (m_entityRenderer.ShouldNotDraw(entity))
            return;

        // Not in front 180 FOV
        if (m_renderData.OccludePos.HasValue)
        {
            Vec2D entityToTarget = entity.Position.XY - m_renderData.OccludePos.Value;
            if (entityToTarget.Dot(m_renderData.ViewDirection) < 0)
                return;
        }

        double dx = Math.Max(entity.Position.X - m_renderData.ViewPos.X, Math.Max(0, m_renderData.ViewPos.X - entity.Position.X));
        double dy = Math.Max(entity.Position.Y - m_renderData.ViewPos.Y, Math.Max(0, m_renderData.ViewPos.Y - entity.Position.Y));
        if (dx * dx + dy * dy > m_renderData.MaxDistance * m_renderData.MaxDistance)
            return;

        if ((m_spriteTransparency && entity.Definition.Properties.Alpha < 1) || entity.Definition.Flags.Shadow)
        {
            entity.RenderDistance = entity.Position.XY.Distance(m_renderData.ViewPos);
            m_alphaEntities.Add(entity);
            return;
        }

        m_entityRenderer.RenderEntity(m_viewSector, entity, m_renderData.ViewPos3D);
    }

    void RenderSector(Sector sector)
    {
        if (sector.CheckCount == m_renderData.CheckCount)
            return;

        m_geometryRenderer.RenderSector(m_viewSector, sector, m_renderData.ViewPos3D);
        sector.CheckCount = m_renderData.CheckCount;
    }

    protected override void PerformRender(IWorld world, RenderInfo renderInfo)
    {
        m_spriteTransparency = m_config.Render.SpriteTransparency;
        Clear(world, renderInfo);

        SetOccludePosition(renderInfo.Camera.Position.Double, renderInfo.Camera.YawRadians, renderInfo.Camera.PitchRadians,
            ref m_occlude, ref m_occludeViewPos);
        if (m_config.Render.Blockmap)
            IterateBlockmap(world, renderInfo);
        else
            TraverseBsp(world, renderInfo);

        PopulatePrimitives(world);

        m_geometryRenderer.RenderPortalsAndSkies(renderInfo);
        m_entityRenderer.RenderNonAlpha(renderInfo);

        m_program.Bind();
        GL.ActiveTexture(TextureUnit.Texture0);
        SetUniforms(renderInfo);
        m_worldDataManager.DrawNonAlpha();
        m_geometryRenderer.RenderStaticGeometry();
        m_program.Unbind();

        m_entityRenderer.RenderAlpha(renderInfo);

        if (m_config.Render.TextureTransparency)
        {
            m_program.Bind();
            GL.ActiveTexture(TextureUnit.Texture0);
            SetUniforms(renderInfo);
            m_worldDataManager.DrawAlpha();
            m_program.Unbind();
        }
        
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
        m_viewClipper.Clear();

        m_geometryRenderer.Clear(renderInfo.TickFraction, newTick);

        if (newTick)
        {
            m_entityRenderer.Clear(world);
            m_worldDataManager.Clear();
        }
    }

    private void TraverseBsp(IWorld world, RenderInfo renderInfo)
    {
        Vec2D position = renderInfo.Camera.Position.XY.Double;
        Vec3D position3D = renderInfo.Camera.Position.Double;
        Vec2D viewDirection = renderInfo.Camera.Direction.XY.Double;
        m_viewSector = world.BspTree.ToSector(position3D);

        m_entityRenderer.SetViewDirection(renderInfo.ViewerEntity, viewDirection);
        m_viewClipper.Center = position;
        m_renderCount = ++world.CheckCounter;
        RecursivelyRenderBsp((uint)world.BspTree.Nodes.Length - 1, position3D, viewDirection, world);
    }

    private void RenderAlphaObjects(Vec2D position, Vec3D position3D, DynamicArray<IRenderObject> alphaEntities)
    {
        // This will just render based on distance from their center point.
        // Not really correct, but mostly correct enough for now.
        DynamicArray<IRenderObject> alphaObjects = alphaEntities;
        alphaObjects.AddRange(m_geometryRenderer.AlphaSides);
        alphaObjects.Sort(m_renderObjectComparer);
        for (int i = 0; i < alphaObjects.Length; i++)
        {
            IRenderObject renderObject = alphaObjects[i];
            if (renderObject.Type == RenderObjectType.Entity)
            {
                Entity entity = (Entity)renderObject;
                m_entityRenderer.RenderEntity(m_viewSector, entity, position3D);
            }
            else if (renderObject.Type == RenderObjectType.Side)
            {
                Side side = (Side)renderObject;
                m_geometryRenderer.RenderAlphaSide(side, side.Line.Segment.OnRight(position));
            }
        }
    }

    private bool Occluded(in Box2D box, in Vec2D position, in Vec2D viewDirection)
    {
        if (box.Contains(position))
            return false;

        if (m_config.Render.MaxDistance > 0)
        {
            int max = m_config.Render.MaxDistance;
            double dx = Math.Max(box.Min.X - position.X, Math.Max(0, position.X - box.Max.X));
            double dy = Math.Max(box.Min.Y - position.Y, Math.Max(0, position.Y - box.Max.Y));
            if (dx * dx + dy * dy > max * max)
                return true;
        }

        if (m_occlude && !box.InView(m_occludeViewPos, viewDirection))
            return true;

        (Vec2D first, Vec2D second) = box.GetSpanningEdge(position);
        return m_viewClipper.InsideAnyRange(first, second);
    }

    private unsafe void RecursivelyRenderBsp(uint nodeIndex, in Vec3D position, in Vec2D viewDirection, IWorld world)
    {
        Vec2D pos2D = position.XY;
        while ((nodeIndex & BspNodeCompact.IsSubsectorBit) == 0)
        {
            fixed (BspNodeCompact* node = &world.BspTree.Nodes[nodeIndex])
            {
                if (Occluded(node->BoundingBox, pos2D, viewDirection))
                    return;

                int front = Convert.ToInt32(node->Splitter.PerpDot(pos2D) < 0);
                int back = front ^ 1;

                RecursivelyRenderBsp(node->Children[front], position, viewDirection, world);
                nodeIndex = node->Children[back];
            }
        }

        Subsector subsector = world.BspTree.Subsectors[nodeIndex & BspNodeCompact.SubsectorMask];
        if (Occluded(subsector.BoundingBox, pos2D, viewDirection))
            return;

        bool hasRenderedSector = subsector.Sector.CheckCount == m_renderCount;
        m_geometryRenderer.RenderSubsector(m_viewSector, subsector, position, hasRenderedSector);

        // Entities are rendered by the sector
        if (hasRenderedSector)
            return;
        subsector.Sector.CheckCount = m_renderCount;
        m_entityRenderer.RenderSubsector(m_viewSector, subsector, position);
    }

    private void PopulatePrimitives(IWorld world)
    {
        const float MaxAlpha = 1.0f;

        if (m_config.Developer.Render.Tracers)
        {
            foreach (PlayerTracerInfo info in world.Player.Tracers)
            {
                if (world.Gametick - info.Gametick > PlayerTracers.TracerRenderTicks)
                    continue;

                AddSeg(info.AimPath, PlayerTracers.AimColor);

                if (info.AimPath != info.LookPath)
                    AddSeg(info.LookPath, PlayerTracers.LookColor);

                foreach (Seg3D tracer in info.Tracers)
                    AddSeg(tracer, PlayerTracers.TracerColor);
            }
        }

        void AddSeg(Seg3D segment, Vec3F color)
        {
            Seg3F seg = (segment.Start.Float, segment.End.Float);
            m_primitiveRenderer.AddSegment(seg, color, MaxAlpha);
        }
    }

    private void SetUniforms(RenderInfo renderInfo)
    {
        // We divide by 4 to make it so the noise changes every four ticks.
        // We then mod by 8 so that the number stays small (or else when it
        // is multiplied in the shader it will overflow very quickly if we
        // don't do this). This could be any number, I just arbitrarily
        // chose 8. This means there are 8 different versions that are to
        // be rendered if the person stares at an unmoving body long enough.
        // Then we add 1 because if the value is 0, then the noise formula
        // outputs zero uniformly which makes it look invisible.
        const int TicksPerFrame = 4;
        const int DifferentFrames = 8;
        
        float timeFrac = ((renderInfo.ViewerEntity.World.Gametick / TicksPerFrame) % DifferentFrames) + 1;
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

        m_program.BoundTexture(TextureUnit.Texture0);
        m_program.HasInvulnerability(drawInvulnerability);
        m_program.Mvp(Renderer.CalculateMvpMatrix(renderInfo));
        m_program.MvpNoPitch(Renderer.CalculateMvpMatrix(renderInfo, true));
        m_program.TimeFrac(renderInfo.TickFraction);
        m_program.FuzzFrac(timeFrac);
        m_program.LightLevelMix(mix);
        m_program.ExtraLight(extraLight);
    }

    private void ReleaseUnmanagedResources()
    {
        m_program.Dispose();
        m_geometryRenderer.Dispose();
        m_worldDataManager.Dispose();
        m_automapRenderer.Dispose();
        m_primitiveRenderer.Dispose();
    }
}
