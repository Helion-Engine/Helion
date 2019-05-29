using Helion.Render.OpenGL.Shared.Buffer.Vao;
using Helion.Render.OpenGL.Shared.Buffer.Vbo;
using Helion.Render.OpenGL.Shared.Shader;
using Helion.Render.OpenGL.Legacy.Texture;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Legacy.Renderers.Console
{
    public class ConsoleRenderer : IDisposable
    {
        protected bool disposed;
        private GLLegacyTextureManager textureManager;
        private ShaderProgram shaderProgram = ConsoleShader.CreateShaderProgramOrThrow();
        private StreamVertexBuffer<ConsoleVertex> vbo = new StreamVertexBuffer<ConsoleVertex>();
        private VertexArrayObject vao = new VertexArrayObject(
            new VaoAttributeF("pos", 0, 2, VertexAttribPointerType.Float),
            new VaoAttributeF("uv", 1, 2, VertexAttribPointerType.Float)
        );

        public ConsoleRenderer(GLLegacyTextureManager glTextureManager)
        {
            textureManager = glTextureManager;
            vao.BindAttributesTo(vbo);
        }

        ~ConsoleRenderer()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                vbo.Dispose();
                vao.Dispose();
                shaderProgram.Dispose();
            }

            disposed = true;
        }

        public void Render()
        {
            // TODO
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
