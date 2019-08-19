using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using Helion.Render.Shared;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.World;

namespace Helion.Render.OpenGL.Renderers.Legacy.World
{
    public class LegacyWorldRenderer : WorldRenderer
    {
        public static readonly VertexArrayAttributes Attributes = new VertexArrayAttributes(
            new VertexPointerFloatAttribute("pos", 0, 3),
            new VertexPointerFloatAttribute("uv", 1, 2),
            new VertexPointerFloatAttribute("lightLevel", 2, 1));

        private readonly Config m_config;
        private readonly IGLFunctions gl;
        private readonly LegacyGLTextureManager m_textureManager;
        private readonly LegacyShader m_shaderProgram;
        private readonly GeometryManager m_geometryManager;

        public LegacyWorldRenderer(Config config, GLCapabilities capabilities, IGLFunctions functions, LegacyGLTextureManager textureManager)
        {
            m_config = config;
            gl = functions;
            m_textureManager = textureManager;
            m_geometryManager = new GeometryManager(capabilities, functions, textureManager);

            using (ShaderBuilder shaderBuilder = LegacyShader.MakeBuilder(functions))
                m_shaderProgram = new LegacyShader(functions, shaderBuilder, Attributes);
        }

        public override void Dispose()
        {
            m_geometryManager.Dispose();
            m_shaderProgram.Dispose();
        }

        protected override void UpdateToNewWorld(WorldBase world)
        {
            m_geometryManager.UpdateTo(world);
        }

        protected override void PerformRender(WorldBase world, RenderInfo renderInfo)
        {
            m_shaderProgram.Bind();
            
            SetUniforms(renderInfo);
            gl.ActiveTexture(TextureUnitType.Zero);

            m_geometryManager.Render(world, renderInfo);

            m_shaderProgram.Unbind();
        }
        
        private void SetUniforms(RenderInfo renderInfo)
        {
            float fovX = (float)MathHelper.ToRadians(m_config.Engine.Render.FieldOfView);
            
            m_shaderProgram.BoundTexture.Set(gl, 0);
            m_shaderProgram.Mvp.Set(gl, GLRenderer.CalculateMvpMatrix(renderInfo, fovX));
        }
    }
}