using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using Helion.Render.Shared;
using Helion.World;

namespace Helion.Render.OpenGL.Renderers.Legacy.World
{
    public class LegacyWorldRenderer : WorldRenderer
    {
        private static readonly VertexArrayAttributes Attributes = new VertexArrayAttributes(
            new VertexPointerFloatAttribute("pos", 0, 3),
            new VertexPointerFloatAttribute("uv", 1, 2));

        private readonly IGLFunctions gl;
        private readonly LegacyGLTextureManager m_textureManager;
        private readonly VertexArrayObject m_vao;
        private readonly VertexBufferObject<SimpleVertex> m_vbo;
        private readonly SimpleShader m_shaderProgram;

        public LegacyWorldRenderer(GLCapabilities capabilities, IGLFunctions functions, LegacyGLTextureManager textureManager)
        {
            gl = functions;
            m_textureManager = textureManager;
            m_vao = new VertexArrayObject(capabilities, functions, Attributes, "VAO: Test stuff");
            m_vbo = new StaticVertexBuffer<SimpleVertex>(capabilities, functions, m_vao, "VBO: Test stuff");

            using (ShaderBuilder shaderBuilder = SimpleShader.MakeBuilder(functions))
                m_shaderProgram = new SimpleShader(functions, shaderBuilder, Attributes);
            
            m_vbo.Add(new SimpleVertex(-0.5f, -0.5f, 0.0f, 0.0f, 1.0f), // Left
                      new SimpleVertex(0.5f, -0.5f, 0.0f, 1.0f, 1.0f), // Right
                      new SimpleVertex(0.0f,  0.5f, 0.0f, 0.5f, 0.0f)); // Top
        }

        public override void Render(WorldBase world, RenderInfo renderInfo)
        {
            m_vbo.UploadIfNeeded();
            
            m_shaderProgram.BindAnd(() =>
            {
                m_shaderProgram.Texture.Set(gl, 0);
                
                m_textureManager.GetFlat("FLAT20").BindAnd(() =>
                {
                    m_vao.BindAnd(() =>
                    {
                        m_vbo.DrawArrays();
                    });
                });
            });
        }

        public override void Dispose()
        {
            m_vao.Dispose();
            m_vbo.Dispose();
            m_shaderProgram.Dispose();
        }
    }
}