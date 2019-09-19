using System;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using Helion.Render.Shared;
using Helion.Render.Shared.World;
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

        private readonly Config m_config;
        private readonly IGLFunctions gl;
        private readonly StreamVertexBuffer<SkyGeometryVertex> m_geometryVbo;
        private readonly VertexArrayObject m_geometryVao;
        private readonly SkySphereGeometryShader m_geometryShaderProgram;
        private readonly SkySphereRenderer m_skySphereRenderer;

        public bool HasGeometry => !m_geometryVbo.Empty;

        public SkySphereComponent(Config config, ArchiveCollection archiveCollection, GLCapabilities capabilities,
            IGLFunctions functions, LegacyGLTextureManager textureManager)
        {
            m_config = config;
            gl = functions;
            m_skySphereRenderer = new SkySphereRenderer(archiveCollection, capabilities, functions, textureManager);
    
            m_geometryVao = new VertexArrayObject(capabilities, functions, GeometryAttributes, "VAO: Sky sphere geometry");
            m_geometryVbo = new StreamVertexBuffer<SkyGeometryVertex>(capabilities, functions, m_geometryVao, "VBO: Sky sphere geometry");
            using (ShaderBuilder builder = SkySphereGeometryShader.MakeBuilder(functions))
                m_geometryShaderProgram = new SkySphereGeometryShader(functions, builder, GeometryAttributes);
        }

        ~SkySphereComponent()
        {
            FailedToDispose(this);
            ReleaseUnmanagedResources();
        }
        
        public void Clear()
        {
            m_geometryVbo.Clear();
        }

        public void Add(WorldVertex first, WorldVertex second, WorldVertex third)
        {
            // TODO: Do some kind of triangle addition instead of one by one.
            m_geometryVbo.Add(new SkyGeometryVertex(first));
            m_geometryVbo.Add(new SkyGeometryVertex(second));
            m_geometryVbo.Add(new SkyGeometryVertex(third));
        }

        public void RenderWorldGeometry(RenderInfo renderInfo)
        {
            m_geometryShaderProgram.Bind();

            float fovX = (float)MathHelper.ToRadians(m_config.Engine.Render.FieldOfView);
            m_geometryShaderProgram.Mvp.Set(gl, GLRenderer.CalculateMvpMatrix(renderInfo, fovX));
            
            m_geometryVbo.UploadIfNeeded();
            
            m_geometryVao.Bind();
            m_geometryVbo.DrawArrays();
            m_geometryVao.Unbind();
            
            m_geometryShaderProgram.Unbind();
        }

        public void RenderSky(RenderInfo renderInfo)
        {
            m_skySphereRenderer.Render(renderInfo);
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

            m_skySphereRenderer.Dispose();
        }
    }
}