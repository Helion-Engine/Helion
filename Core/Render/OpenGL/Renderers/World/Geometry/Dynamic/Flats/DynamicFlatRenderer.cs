using System;
using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Util;
using Helion.Resources.Images;
using Helion.Util.Extensions;
using Helion.World.Bsp;
using OpenTK;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Dynamic.Flats
{
    public class DynamicFlatRenderer : IDisposable
    {
        private static VertexArrayAttributes Attributes = new VertexArrayAttributes(
            new VertexFloatAttribute("pos", 0, 2),
            new VertexIntAttribute("textureTableIndex", 1, 1),
            new VertexIntAttribute("planeIndex", 2, 1));
        
        private readonly GLTextureManager m_textureManager;
        private readonly VertexArrayObject m_vao;
        private readonly DynamicVertexBufferObject<DynamicFlatVertex> m_vbo;
        private readonly DynamicFlatShaderProgram m_shaderProgram;
        
        public DynamicFlatRenderer(GLCapabilities capabilities, GLTextureManager textureManager)
        {
            m_textureManager = textureManager;
            m_vao = new VertexArrayObject(capabilities, Attributes, "Dynamic World Flat VAO");
            m_vbo = new DynamicVertexBufferObject<DynamicFlatVertex>(capabilities, m_vao, "Dynamic World Flat VBO");
            m_shaderProgram = DynamicFlatShaderProgram.MakeShaderProgram(Attributes);
        }

        ~DynamicFlatRenderer()
        {
            ReleaseUnmanagedResources();
        }

        public void AddSubsector(Subsector subsector, IImageRetriever imageRetriever)
        {
            // TODO
        }

        public void Render(Matrix4 mvp)
        {
            m_shaderProgram.BindAnd(() =>
            {
                m_shaderProgram.Mvp.Set(mvp);
                m_shaderProgram.TextureAtlas.Set(GLConstants.TextureAtlasUnit.ToIndex());
//                m_shaderProgram.TextureInfoBuffer.Set(GLConstants.TextureInfoUnit.ToIndex());

                m_textureManager.BindAnd(() =>
                {
                    m_textureManager.TextureDataBuffer.BindAnd(() =>
                    {
                        m_vao.BindAnd(() =>
                        {
                            m_vbo.BindAnd(() =>
                            {
                                if (m_vbo.NeedsUpload)
                                    m_vbo.Upload();
                                
                                m_vbo.DrawArrays();
                            });
                        });
                    });
                });
            });
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            m_vbo.Dispose();
            m_vao.Dispose();
            m_shaderProgram.Dispose();
        }
    }
}