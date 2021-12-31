using System;
using System.Collections.Generic;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;
using Helion.Render.Legacy.Renderers.Legacy.World.Automap;
using Helion.Render.Legacy.Renderers.Legacy.World.Data;
using Helion.Render.Legacy.Renderers.Legacy.World.Entities;
using Helion.Render.Legacy.Renderers.Legacy.World.Geometry;
using Helion.Render.Legacy.Shader;
using Helion.Render.Legacy.Shared;
using Helion.Render.Legacy.Shared.World.ViewClipping;
using Helion.Render.Legacy.Texture.Legacy;
using Helion.Render.Legacy.Vertex;
using Helion.Render.Legacy.Vertex.Attribute;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.World;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Renderers.Legacy.World;

public class LegacyWorldRenderer : WorldRenderer
{
    public static readonly VertexArrayAttributes Attributes = new(
        new VertexPointerFloatAttribute("pos", 0, 3),
        new VertexPointerFloatAttribute("uv", 1, 2),
        new VertexPointerFloatAttribute("lightLevel", 2, 1),
        new VertexPointerFloatAttribute("alpha", 3, 1),
        new VertexPointerFloatAttribute("colorMul", 4, 3),
        new VertexPointerFloatAttribute("fuzz", 5, 1));

    private readonly IConfig m_config;
    private readonly IGLFunctions gl;
    private readonly GeometryRenderer m_geometryRenderer;
    private readonly EntityRenderer m_entityRenderer;
    private readonly LegacyShader m_shaderProgram;
    private readonly RenderWorldDataManager m_worldDataManager;
    private readonly LegacyAutomapRenderer m_automapRenderer;
    private readonly ViewClipper m_viewClipper = new();

    public LegacyWorldRenderer(IConfig config, ArchiveCollection archiveCollection, GLCapabilities capabilities,
        IGLFunctions functions, LegacyGLTextureManager textureManager)
    {
        m_config = config;
        gl = functions;
        m_automapRenderer = new LegacyAutomapRenderer(capabilities, gl, archiveCollection);
        m_worldDataManager = new RenderWorldDataManager(capabilities, gl);
        m_entityRenderer = new EntityRenderer(config, textureManager, m_worldDataManager);
        m_geometryRenderer = new GeometryRenderer(config, archiveCollection, capabilities, functions,
            textureManager, m_viewClipper, m_worldDataManager);
        m_viewSector = Sector.CreateDefault();

        using (ShaderBuilder shaderBuilder = LegacyShader.MakeBuilder(functions))
            m_shaderProgram = new LegacyShader(functions, shaderBuilder, Attributes);
    }

    ~LegacyWorldRenderer()
    {
        FailedToDispose(this);
        ReleaseUnmanagedResources();
    }

    public override void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    protected override void UpdateToNewWorld(WorldBase world)
    {
        m_geometryRenderer.UpdateTo(world);
        m_entityRenderer.UpdateTo(world);
    }

    protected override void PerformAutomapRender(WorldBase world, RenderInfo renderInfo)
    {
        Clear(world, renderInfo);
        TraverseBsp(world, renderInfo);

        m_automapRenderer.Render(world, renderInfo);
    }

    protected override void PerformRender(WorldBase world, RenderInfo renderInfo)
    {
        Clear(world, renderInfo);

        TraverseBsp(world, renderInfo);
        RenderWorldData(renderInfo);
        m_geometryRenderer.Render(renderInfo);
    }

    private void Clear(WorldBase world, RenderInfo renderInfo)
    {
        m_viewClipper.Clear();
        m_worldDataManager.Clear();

        m_geometryRenderer.Clear(renderInfo.TickFraction);
        m_entityRenderer.Clear(world, renderInfo.TickFraction, renderInfo.ViewerEntity);
    }

    private Sector m_viewSector;

    private void TraverseBsp(WorldBase world, RenderInfo renderInfo)
    {
        Vec2D position = renderInfo.Camera.Position.XY.Double;
        Vec3D position3D = renderInfo.Camera.Position.Double;
        Vec2D viewDirection = renderInfo.Camera.Direction.XY.Double;
        m_viewSector = world.BspTree.ToSubsector(position3D).Sector;

        m_viewClipper.Center = position;
        RecursivelyRenderBsp(world.BspTree.Root, position3D, viewDirection, world);

        // This will just render based on distance from their center point.
        // Not really correct, but mostly correct enough for now.
        List<IRenderObject> alphaObjects = m_entityRenderer.AlphaEntities;
        alphaObjects.AddRange(m_geometryRenderer.AlphaSides);
        alphaObjects.Sort((i1, i2) => i2.RenderDistance.CompareTo(i1.RenderDistance));
        for (int i = 0; i < alphaObjects.Count; i++)
        {
            IRenderObject renderObject = alphaObjects[i];
            if (renderObject.Type == RenderObjectType.Entity)
            {
                m_entityRenderer.RenderEntity(m_viewSector, (Entity)renderObject, position3D, viewDirection);
            }
            else if (renderObject.Type == RenderObjectType.Side)
            {
                Side side = (Side)renderObject;
                m_geometryRenderer.RenderAlphaSide(side, side.Line.Segment.OnRight(position), position3D);
            }
        }
    }

    private bool Occluded(in Box2D box, in Vec2D position)
    {
        if (box.Contains(position))
            return false;

        (Vec2D first, Vec2D second) = box.GetSpanningEdge(position);
        return m_viewClipper.InsideAnyRange(first, second);
    }

    private void RecursivelyRenderBsp(in BspNodeCompact node, in Vec3D position, in Vec2D viewDirection,
        WorldBase world)
    {
        if (Occluded(node.BoundingBox, position.XY))
            return;
        
        // TODO: Consider changing to xor trick to avoid branching?
        if (node.Splitter.OnRight(position))
        {
            if (node.IsRightSubsector)
                RenderSubsector(world.BspTree.Subsectors[node.RightChildAsSubsector], position, viewDirection);
            else
                RecursivelyRenderBsp(world.BspTree.Nodes[node.RightChild], position, viewDirection, world);

            if (node.IsLeftSubsector)
                RenderSubsector(world.BspTree.Subsectors[node.LeftChildAsSubsector], position, viewDirection);
            else
                RecursivelyRenderBsp(world.BspTree.Nodes[node.LeftChild], position, viewDirection, world);
        }
        else
        {
            if (node.IsLeftSubsector)
                RenderSubsector(world.BspTree.Subsectors[node.LeftChildAsSubsector], position, viewDirection);
            else
                RecursivelyRenderBsp(world.BspTree.Nodes[node.LeftChild], position, viewDirection, world);

            if (node.IsRightSubsector)
                RenderSubsector(world.BspTree.Subsectors[node.RightChildAsSubsector], position, viewDirection);
            else
                RecursivelyRenderBsp(world.BspTree.Nodes[node.RightChild], position, viewDirection, world);
        }
    }

    private void RenderSubsector(Subsector subsector, in Vec3D position, in Vec2D viewDirection)
    {
        if (Occluded(subsector.BoundingBox, position.XY))
            return;

        m_geometryRenderer.RenderSubsector(m_viewSector, subsector, position);
        m_entityRenderer.RenderSubsector(m_viewSector, subsector, position, viewDirection);
    }

    private void SetUniforms(RenderInfo renderInfo)
    {
        int drawInvulnerability = 0;

        // We divide by 4 to make it so the noise changes every four ticks.
        // We then mod by 8 so that the number stays small (or else when it
        // is multiplied in the shader it will overflow very quickly if we
        // don't do this). This could be any number, I just arbitrarily
        // chose 8. This means there are 8 different versions that are to
        // be rendered if the person stares at an unmoving body long enough.
        // Then we add 1 because if the value is 0, then the noise formula
        // outputs zero uniformly which makes it look invisible.
        const int ticksPerFrame = 4;
        const int differentFrames = 8;
        float timeFrac = ((renderInfo.ViewerEntity.World.Gametick / ticksPerFrame) % differentFrames) + 1;
        int extraLight = 0;
        float mix = 0.0f;

        if (renderInfo.ViewerEntity.PlayerObj != null)
        {
            if (renderInfo.ViewerEntity.PlayerObj.DrawFullBright())
                mix = 1.0f;
            if (renderInfo.ViewerEntity.PlayerObj.DrawInvulnerableColorMap())
                drawInvulnerability = 1;

            extraLight = renderInfo.ViewerEntity.PlayerObj.ExtraLight * Constants.ExtraLightFactor;
        }

        m_shaderProgram.BoundTexture.Set(gl, 0);
        m_shaderProgram.HasInvulnerability.Set(gl, drawInvulnerability);
        m_shaderProgram.LightDropoff.Set(gl, m_config.Render.LightDropoff ? 1 : 0);
        m_shaderProgram.Mvp.Set(gl, GLLegacyRenderer.CalculateMvpMatrix(renderInfo));
        m_shaderProgram.MvpNoPitch.Set(gl, GLLegacyRenderer.CalculateMvpMatrix(renderInfo, true));
        m_shaderProgram.TimeFrac.Set(gl, timeFrac);
        m_shaderProgram.LightLevelMix.Set(gl, mix);
        m_shaderProgram.ExtraLight.Set(gl, extraLight);
    }

    private void RenderWorldData(RenderInfo renderInfo)
    {
        m_shaderProgram.Bind();

        SetUniforms(renderInfo);
        gl.ActiveTexture(TextureUnitType.Zero);
        m_worldDataManager.Draw();

        m_shaderProgram.Unbind();
    }

    private void ReleaseUnmanagedResources()
    {
        m_shaderProgram.Dispose();
        m_geometryRenderer.Dispose();
        m_worldDataManager.Dispose();
        m_automapRenderer.Dispose();
    }
}
