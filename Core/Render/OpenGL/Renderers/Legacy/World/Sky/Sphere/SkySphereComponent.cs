using System;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using Helion.Render.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere
{
    public class SkySphereComponent : ISkyComponent
    {
        private static readonly VertexArrayAttributes GeometryAttributes = new VertexArrayAttributes(
            new VertexPointerFloatAttribute("pos", 0, 3));
        private static readonly VertexArrayAttributes SphereAttributes = new VertexArrayAttributes(
            new VertexPointerFloatAttribute("pos", 0, 3),
            new VertexPointerFloatAttribute("uv", 1, 2));

        private readonly Config m_config;
        private readonly IGLFunctions gl;
        private readonly StaticVertexBuffer<SkySphereVertex> m_sphereVbo;
        private readonly VertexArrayObject m_sphereVao;
        private readonly SkySphereShader m_sphereShaderProgram;
        private readonly StreamVertexBuffer<SkyGeometryVertex> m_geometryVbo;
        private readonly VertexArrayObject m_geometryVao;
        private readonly SkySphereGeometryShader m_geometryShaderProgram;
        private readonly SkySphereTextures m_skyTextures;

        public bool HasGeometry => !m_geometryVbo.Empty;

        public SkySphereComponent(Config config, ArchiveCollection archiveCollection, GLCapabilities capabilities,
            IGLFunctions functions, LegacyGLTextureManager textureManager)
        {
            m_config = config;
            gl = functions;
            m_skyTextures = new SkySphereTextures(archiveCollection, functions, textureManager);
            
            m_geometryVao = new VertexArrayObject(capabilities, functions, GeometryAttributes, "VAO: Sky sphere geometry");
            m_geometryVbo = new StreamVertexBuffer<SkyGeometryVertex>(capabilities, functions, m_geometryVao, "VBO: Sky sphere geometry");
            using (ShaderBuilder builder = SkySphereGeometryShader.MakeBuilder(functions))
                m_geometryShaderProgram = new SkySphereGeometryShader(functions, builder, GeometryAttributes);
            
            m_sphereVao = new VertexArrayObject(capabilities, functions, SphereAttributes, "VAO: Sky sphere");
            m_sphereVbo = new StaticVertexBuffer<SkySphereVertex>(capabilities, functions, m_sphereVao, "VBO: Sky sphere");
            using (ShaderBuilder builder = SkySphereShader.MakeBuilder(functions))
                m_sphereShaderProgram = new SkySphereShader(functions, builder, SphereAttributes);
        }

        ~SkySphereComponent()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }
        
        public void Clear()
        {
            m_geometryVbo.Clear();
        }

        public void Add(LegacyVertex first, LegacyVertex second, LegacyVertex third)
        {
            m_geometryVbo.Add(new SkyGeometryVertex(first));
            m_geometryVbo.Add(new SkyGeometryVertex(second));
            m_geometryVbo.Add(new SkyGeometryVertex(third));
        }

        public void RenderWorldGeometry(RenderInfo renderInfo)
        {
            m_geometryShaderProgram.Bind();

            float fovX = (float)MathHelper.ToRadians(m_config.Engine.Render.FieldOfView);
            m_geometryShaderProgram.Mvp.Set(gl, GLRenderer.CalculateMvpMatrix(renderInfo, fovX));
            
            m_geometryVao.Bind();
            m_geometryVbo.DrawArrays();
            m_geometryVao.Unbind();
            
            m_geometryShaderProgram.Unbind();
        }

        public void RenderSky(RenderInfo renderInfo)
        {
            m_sphereShaderProgram.Bind();

            gl.ActiveTexture(TextureUnitType.Zero);
            m_sphereShaderProgram.BoundTexture.Set(gl, 0);

            m_skyTextures.GetUpperSky().Bind();
            m_sphereVao.Bind();
            m_sphereVbo.DrawArrays();
            m_sphereVao.Unbind();
            m_skyTextures.GetUpperSky().Unbind();

            m_skyTextures.GetLowerSky().Bind();
            // TODO
            //m_sphereVao.Bind();
            //m_sphereVbo.DrawArrays();
            //m_sphereVao.Unbind();
            m_skyTextures.GetLowerSky().Unbind();
            
            m_sphereShaderProgram.Unbind();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
        
        private void ReleaseUnmanagedResources()
        {
            m_geometryShaderProgram.Dispose();
            m_geometryVbo.Dispose();
            m_geometryVao.Dispose();

            m_sphereShaderProgram.Dispose();
            m_sphereVao.Dispose();
            m_sphereVbo.Dispose();
            
            m_skyTextures.Dispose();
        }
    }
}