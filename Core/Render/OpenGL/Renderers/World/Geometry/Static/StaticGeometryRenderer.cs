using System;
using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Util;
using Helion.World;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static
{
    public class StaticGeometryRenderer : IDisposable
    {
        private static VertexArrayAttributes Attributes = new VertexArrayAttributes(
            new VertexFloatAttribute("pos", 0, 2),
            new VertexFloatAttribute("u", 1, 1),
            new VertexIntAttribute("floorPlaneIndex", 2, 1),
            new VertexIntAttribute("ceilingPlaneIndex", 3, 1),
            new VertexIntAttribute("wallIndex", 4, 1),
            new VertexIntAttribute("flags", 5, 1)
        );
        
        private readonly GLTextureManager m_textureManager;
        private readonly VertexArrayObject m_vao;
        private readonly DynamicVertexBufferObject<StaticWorldVertex> m_vbo;
        private readonly StaticWorldShaderProgram m_shaderProgram;

        public StaticGeometryRenderer(GLCapabilities capabilities, GLTextureManager textureManager)
        {
            m_textureManager = textureManager;
            m_vao = new VertexArrayObject(Attributes);
            m_vbo = new DynamicVertexBufferObject<StaticWorldVertex>(capabilities, m_vao, "Static World VBO");
            m_shaderProgram = StaticWorldShaderProgram.MakeShaderProgram(Attributes);
        }

        ~StaticGeometryRenderer()
        {
            ReleaseUnmanagedResources();
        }
        
        public void Render(WorldBase world)
        {
            m_shaderProgram.BindAnd(() =>
            {
                m_vao.BindAnd(() =>
                {
                    m_vbo.DrawArrays();
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