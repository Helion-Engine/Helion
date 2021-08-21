using System;
using GlmSharp;
using Helion.Render.Common.Context;
using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Pipeline;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Textures.Buffer;
using Helion.World;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static.Walls
{
    public class GLStaticWallGeometryRenderer : IDisposable
    {
        private readonly GLTextureManager m_textureManager;
        private readonly GLTextureDataBuffer m_textureDataBuffer;
        private readonly RenderPipeline<GLStaticWallGeometryShader, GLStaticWallGeometryVertex> m_pipeline;
        private bool m_disposed;

        public GLStaticWallGeometryRenderer(GLTextureManager textureManager, GLTextureDataBuffer textureDataBuffer)
        {
            m_textureManager = textureManager;
            m_textureDataBuffer = textureDataBuffer;
            m_pipeline = new("Static geometry (walls)", BufferUsageHint.DynamicDraw, PrimitiveType.Triangles);
        }
        
        ~GLStaticWallGeometryRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }
        
        public void UpdateTo(IWorld world)
        {
            VertexBufferObject<GLStaticWallGeometryVertex> vbo = m_pipeline.Vbo;
            vbo.Clear();
            
            // TODO
        }
        
        public void Render(WorldRenderContext context)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            m_textureManager.GetAtlas(0).Bind();
            
            GL.ActiveTexture(TextureUnit.Texture1);
            m_textureDataBuffer.Texture.Bind();

            m_pipeline.Draw(shader => 
            {
                shader.Mvp.Set(mat4.Identity);
                shader.Tex.Set(TextureUnit.Texture0);
                shader.Data.Set(TextureUnit.Texture1);
            });

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }
        
        private void PerformDispose()
        {
            if (m_disposed)
                return;

            m_pipeline.Dispose();

            m_disposed = true;
        }
    }
}
