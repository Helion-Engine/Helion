using System;
using System.Drawing;
using Helion.Geometry.Quads;
using Helion.Render.Common;
using Helion.Render.Common.Context;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Pipeline;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Images
{
    public class ImageWorldRenderer : IDisposable
    {
        private readonly RenderPipeline<ImageWorldShader, ImageWorldVertex> m_pipeline;
        private bool m_disposed;

        public ImageWorldRenderer()
        {
            m_pipeline = new RenderPipeline<ImageWorldShader, ImageWorldVertex>(
                "World image primitives", BufferUsageHint.StreamDraw, PrimitiveType.Triangles);
        }

        ~ImageWorldRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void DrawImage(string texture, Quad3D quad, Color? color = null)
        {
            // TODO
        }
        
        public void DrawSurface(string surfaceName, Quad3D quad, Color? color = null)
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
