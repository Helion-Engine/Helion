using Helion.Render.OpenGL.Legacy.Texture;
using Helion.Render.OpenGL.Shared.Buffer.Vao;
using Helion.Render.OpenGL.Shared.Buffer.Vbo;
using Helion.Render.OpenGL.Shared.Shader;
using Helion.World;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Legacy.Renderers.World
{
    public class WorldRenderer : IDisposable
    {
        protected bool disposed;
        private GLLegacyTextureManager textureManager;
        private WeakReference? lastProcessedWorld = null;
        private VertexArrayObject vao = new VertexArrayObject(
            new VaoAttributeF("pos", 0, 3, VertexAttribPointerType.Float),
            new VaoAttributeF("uv", 1, 2, VertexAttribPointerType.Float),
            new VaoAttributeF("alpha", 2, 1, VertexAttribPointerType.Float),
            new VaoAttributeF("unitBrightness", 3, 1, VertexAttribPointerType.Float)
        );
        private StreamVertexBuffer<WorldVertex> vbo = new StreamVertexBuffer<WorldVertex>();
        private ShaderProgram shaderProgram;

        public WorldRenderer(GLLegacyTextureManager glTextureManager)
        {
            textureManager = glTextureManager;
            vao.BindAttributesTo(vbo);
            shaderProgram = WorldShader.CreateShaderProgramOrThrow(vao);
        }

        ~WorldRenderer() => Dispose(false);

        private bool ShouldUpdateToNewWorld(WorldBase world)
        {
            return lastProcessedWorld == null ||
                   !lastProcessedWorld.IsAlive ||
                   !ReferenceEquals(lastProcessedWorld.Target, world);
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

        public void Render(WorldBase world)
        {
            if (ShouldUpdateToNewWorld(world))
            {
                // TODO: Populate data structures
            }

            shaderProgram.BindAnd(() => {
                // TODO: Set uniforms.

                // TODO: Fill VBO for all walls/entities in subsector that is visible
                    // TODO: Bind appropriate texture.
                    // TODO: Draw VBO.
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
