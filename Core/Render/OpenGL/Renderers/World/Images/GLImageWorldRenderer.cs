using System;
using System.Drawing;
using Helion.Geometry.Quads;
using Helion.Render.Common;
using Helion.Render.Common.Context;
using Helion.Render.OpenGL.Pipeline;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Images
{
    public class GLImageWorldRenderer : IDisposable
    {
        private readonly RenderPipeline<GLImageWorldShader, GLImageWorldVertex> m_pipeline;
        private bool m_disposed;

        public GLImageWorldRenderer()
        {
            m_pipeline = new RenderPipeline<GLImageWorldShader, GLImageWorldVertex>(
                "World image primitives", BufferUsageHint.StreamDraw, PrimitiveType.Triangles);
        }

        ~GLImageWorldRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void DrawImage(IRenderableTexture texture, Quad3D quad, Color? color)
        {
            // TODO
        }

        public void Render(WorldRenderContext context)
        {
            // TODO
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
