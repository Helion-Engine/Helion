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
        private VertexArrayObject vao = new VertexArrayObject(
            new VaoAttributeF("pos", 0, 2, VertexAttribPointerType.Float),
            new VaoAttributeF("uv", 1, 2, VertexAttribPointerType.Float)
        );
        private StreamVertexBuffer<ConsoleVertex> vbo = new StreamVertexBuffer<ConsoleVertex>();
        private ShaderProgram shaderProgram;

        public ConsoleRenderer(GLLegacyTextureManager glTextureManager)
        {
            textureManager = glTextureManager;
            shaderProgram = ConsoleShader.CreateShaderProgramOrThrow(vao);
            vao.BindAttributesTo(vbo);
        }

        ~ConsoleRenderer() => Dispose(false);

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

        public void Render(Util.Console console)
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
