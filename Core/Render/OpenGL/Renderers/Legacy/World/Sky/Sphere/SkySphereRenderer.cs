using System;
using GlmSharp;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using Helion.Render.Shared;
using Helion.Resources.Archives.Collection;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere
{
    public class SkySphereRenderer : IDisposable
    {
        private const int HorizontalSpherePoints = 32;
        private const int VerticalSpherePoints = 32;
        
        private static readonly VertexArrayAttributes SphereAttributes = new VertexArrayAttributes(
            new VertexPointerFloatAttribute("pos", 0, 3),
            new VertexPointerFloatAttribute("uv", 1, 2));

        private readonly IGLFunctions gl;
        private readonly StaticVertexBuffer<SkySphereVertex> m_sphereVbo;
        private readonly VertexArrayObject m_sphereVao;
        private readonly SkySphereShader m_sphereShaderProgram;
        private readonly SkySphereTextures m_skyTextures;

        public SkySphereRenderer(ArchiveCollection archiveCollection, GLCapabilities capabilities,
            IGLFunctions functions, LegacyGLTextureManager textureManager)
        {
            gl = functions;
            m_sphereVao = new VertexArrayObject(capabilities, functions, SphereAttributes, "VAO: Sky sphere");
            m_sphereVbo = new StaticVertexBuffer<SkySphereVertex>(capabilities, functions, m_sphereVao, "VBO: Sky sphere");
            using (ShaderBuilder builder = SkySphereShader.MakeBuilder(functions))
                m_sphereShaderProgram = new SkySphereShader(functions, builder, SphereAttributes);
            
            m_skyTextures = new SkySphereTextures(archiveCollection, functions, textureManager);

            GenerateSphereVerticesAndUpload();
        }

        ~SkySphereRenderer()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }

        public void Render(RenderInfo renderInfo)
        {
            m_sphereShaderProgram.Bind();

            gl.ActiveTexture(TextureUnitType.Zero);
            m_sphereShaderProgram.BoundTexture.Set(gl, 0);

            // TODO: Calculate the proper rotation matrix.
            mat4 mvp = mat4.Identity;
            m_sphereShaderProgram.Mvp.Set(gl, mvp);

            DrawHemisphere(m_skyTextures.GetUpperSky());
            //DrawHemisphere(m_skyTextures.GetLowerSky());
            
            m_sphereShaderProgram.Unbind();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void DrawHemisphere(GLLegacyTexture texture)
        {
            texture.Bind();
            m_sphereVao.Bind();
            m_sphereVbo.DrawArrays();
            m_sphereVao.Unbind();
            texture.Unbind();
        }

        private void GenerateSphereVerticesAndUpload()
        {
            SphereTable sphereTable = new SphereTable(HorizontalSpherePoints, VerticalSpherePoints);
            
            for (int row = 0; row < VerticalSpherePoints; row++)
            {
                for (int col = 0; col < HorizontalSpherePoints; col++)
                {
                    // Note that this works fine with the +1, it will not go
                    // out of range because we specifically made sure that the
                    // code adds in one extra vertex for us on both the top row
                    // and the right column.
                    SkySphereVertex bottomLeft = sphereTable.MercatorRectangle[row, col];
                    SkySphereVertex bottomRight = sphereTable.MercatorRectangle[row, col + 1];
                    SkySphereVertex topLeft = sphereTable.MercatorRectangle[row + 1, col];
                    SkySphereVertex topRight = sphereTable.MercatorRectangle[row + 1, col + 1];
                    
                    m_sphereVbo.Add(topLeft);
                    m_sphereVbo.Add(bottomLeft);
                    m_sphereVbo.Add(topRight);
                    
                    m_sphereVbo.Add(topRight);
                    m_sphereVbo.Add(bottomLeft);
                    m_sphereVbo.Add(bottomRight);
                }
            }
            
            m_sphereVbo.UploadIfNeeded();
        }

        private void ReleaseUnmanagedResources()
        {
            m_sphereShaderProgram.Dispose();
            m_sphereVao.Dispose();
            m_sphereVbo.Dispose();
            
            m_skyTextures.Dispose();
        }
    }
}