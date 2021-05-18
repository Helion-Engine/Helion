using System;
using GlmSharp;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Renderers.Legacy.World.Automap;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Renderers.Legacy.World.Entities;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using Helion.Render.Shared;
using Helion.Render.Shared.World.ViewClipping;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.World;
using Helion.World.Bsp;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Subsectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World
{
    public class LegacyWorldRenderer : WorldRenderer
    {
        public static readonly VertexArrayAttributes Attributes = new(
            new VertexPointerFloatAttribute("pos", 0, 3),
            new VertexPointerFloatAttribute("uv", 1, 2),
            new VertexPointerFloatAttribute("lightLevel", 2, 1),
            new VertexPointerFloatAttribute("alpha", 3, 1),
            new VertexPointerFloatAttribute("colorMul", 4, 3),
            new VertexPointerFloatAttribute("fuzz", 5, 1));

        private readonly Config m_config;
        private readonly IGLFunctions gl;
        private readonly GeometryRenderer m_geometryRenderer;
        private readonly EntityRenderer m_entityRenderer;
        private readonly LegacyShader m_shaderProgram;
        private readonly RenderWorldDataManager m_worldDataManager;
        private readonly LegacyAutomapRenderer m_automapRenderer;
        private readonly ViewClipper m_viewClipper = new();

        public LegacyWorldRenderer(Config config, ArchiveCollection archiveCollection, GLCapabilities capabilities,
            IGLFunctions functions, LegacyGLTextureManager textureManager)
        {
            m_config = config;
            gl = functions;
            m_automapRenderer = new LegacyAutomapRenderer(capabilities, gl, archiveCollection);
            m_worldDataManager = new RenderWorldDataManager(capabilities, gl);
            m_entityRenderer = new EntityRenderer(config, textureManager, m_worldDataManager);
            m_geometryRenderer = new GeometryRenderer(config, archiveCollection, capabilities, functions,
                textureManager, m_viewClipper, m_worldDataManager);

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

        private void TraverseBsp(WorldBase world, RenderInfo renderInfo)
        {
            Vec2D position = renderInfo.Camera.Position.XY.Double;
            Vec2D viewDirection = renderInfo.Camera.Direction.XY.Double;

            m_viewClipper.Center = position;
            RecursivelyRenderBsp(world.BspTree.Root, position, viewDirection, world);
            m_entityRenderer.RenderAlphaEntities(position, viewDirection);
        }

        private bool Occluded(in Box2D box, in Vec2D position)
        {
            if (box.Contains(position))
                return false;

            (Vec2D first, Vec2D second) = box.GetSpanningEdge(position);
            return m_viewClipper.InsideAnyRange(first, second);
        }

        private void RecursivelyRenderBsp(in BspNodeCompact node, in Vec2D position, in Vec2D viewDirection,
            WorldBase world)
        {
            if (Occluded(node.BoundingBox, position))
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

        private void RenderSubsector(Subsector subsector, in Vec2D position, in Vec2D viewDirection)
        {
            if (Occluded(subsector.BoundingBox, position))
                return;

            m_geometryRenderer.RenderSubsector(subsector, position);
            m_entityRenderer.RenderSubsector(subsector, position, viewDirection);
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

            if (renderInfo.ViewerEntity is Player player)
            {
                if (player.DrawFullBright())
                    mix = 1.0f;
                if (player.DrawInvulnerableColorMap())
                    drawInvulnerability = 1;

                extraLight = player.ExtraLight * Constants.ExtraLightFactor;
            }

            m_shaderProgram.BoundTexture.Set(gl, 0);
            m_shaderProgram.HasInvulnerability.Set(gl, drawInvulnerability);
            m_shaderProgram.Mvp.Set(gl, GLRenderer.CalculateMvpMatrix(renderInfo));
            m_shaderProgram.TimeFrac.Set(gl, timeFrac);
            m_shaderProgram.Camera.Set(gl, new vec3(renderInfo.Camera.Position.X, renderInfo.Camera.Position.Y,
                renderInfo.Camera.Position.Z));
            m_shaderProgram.LookingAngle.Set(gl, renderInfo.Camera.YawRadians);
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
}