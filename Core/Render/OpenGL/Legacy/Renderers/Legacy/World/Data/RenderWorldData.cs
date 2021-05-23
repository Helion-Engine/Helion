using System;
using Helion.Render.OpenGL.Legacy.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Legacy.Context;
using Helion.Render.OpenGL.Legacy.Texture.Legacy;
using Helion.Render.OpenGL.Legacy.Vertex;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Legacy.Renderers.Legacy.World.Data
{
    public class RenderWorldData : IDisposable
    {
        public readonly GLLegacyTexture Texture;
        public readonly StreamVertexBuffer<LegacyVertex> Vbo;
        public readonly VertexArrayObject Vao;

        public RenderWorldData(GLCapabilities capabilities, IGLFunctions functions, GLLegacyTexture texture)
        {
            Texture = texture;
            Vao = new VertexArrayObject(capabilities, functions, LegacyWorldRenderer.Attributes, $"VAO: Attributes for {texture.Name}");
            Vbo = new StreamVertexBuffer<LegacyVertex>(capabilities, functions, Vao, $"VBO: Geometry for {texture.Name}");
        }

        ~RenderWorldData()
        {
            FailedToDispose(this);
            ReleaseUnmanagedResources();
        }

        public void Clear()
        {
            Vbo.Clear();
        }

        public void Draw()
        {
            if (Vbo.Empty)
                return;

            // We are doing binding manually since apparently these are all
            // coming up in the memory profiler as a bunch of new 'actions'.
            // We don't want GC pressure if there's a lot of textures, since
            // this means we get O(N) actions for N used textures.
            Texture.Bind();
            Vao.Bind();
            Vbo.Bind();
            
            Vbo.Upload();
            Vbo.DrawArrays();
            
            Vbo.Unbind();
            Vao.Unbind();
            Texture.Unbind();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            Vbo.Dispose();
            Vao.Dispose();
        }
    }
}