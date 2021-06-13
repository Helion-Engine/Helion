using System;
using System.Collections.Generic;
using System.Drawing;
using GlmSharp;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Render.Common;
using Helion.Render.Common.Context;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL.Pipeline;
using Helion.Util.Extensions;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Hud
{
    /// <summary>
    /// A renderer for the hud which uses GL backing data.
    /// </summary>
    public class GLHudRenderer : IHudRenderContext
    {
        private readonly RenderPipeline<GLHudShader, GLHudVertex> m_pointPrimitivePipeline;
        private readonly RenderPipeline<GLHudShader, GLHudVertex> m_linePrimitivePipeline;
        private readonly RenderPipeline<GLHudShader, GLHudVertex> m_trianglePrimitivePipeline;
        private readonly RenderTexturePipeline<GLHudShader, GLHudVertex> m_texturePipeline;
        private readonly Stack<VirtualResolutionInfo> m_resolutionStack = new();
        private int m_elementsDrawn;
        private bool m_disposed;

        private VirtualResolutionInfo ResolutionInfo => m_resolutionStack.Peek();
        private Dimension VirtualDimension => ResolutionInfo.Dimension;

        public GLHudRenderer()
        {
            m_pointPrimitivePipeline = new("Hud shader points", BufferUsageHint.StreamDraw, PrimitiveType.Points);
            m_linePrimitivePipeline = new("Hud shader lines", BufferUsageHint.StreamDraw, PrimitiveType.Lines);
            m_trianglePrimitivePipeline = new("Hud shader triangles", BufferUsageHint.StreamDraw, PrimitiveType.Triangles);
            m_texturePipeline = new("Hud shader textures");
        }
        
        ~GLHudRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }
        
        internal void Begin(HudRenderContext context)
        {
            m_elementsDrawn = 0;
            m_resolutionStack.Clear();

            VirtualResolutionInfo info = new(context.Dimension, ResolutionScale.None);
            m_resolutionStack.Push(info);
        }

        public void Clear(Color color)
        {
            DrawBox((Vec2I.Zero, VirtualDimension.Vector), color);
        }

        public void Point(Vec2I point, Color color, Align window = Align.TopLeft)
        {
            // TODO

            m_elementsDrawn++;
        }

        public void Points(Vec2I[] points, Color color, Align window = Align.TopLeft)
        {
            // TODO
            
            m_elementsDrawn++;
        }

        public void Line(Seg2D seg, Color color, Align window = Align.TopLeft)
        {
            // TODO
            
            m_elementsDrawn++;
        }

        public void Lines(Seg2D[] segs, Color color, Align window = Align.TopLeft)
        {
            // TODO
            
            m_elementsDrawn++;
        }

        public void DrawBox(Box2I box, Color color, Align window = Align.TopLeft)
        {
            // TODO
            
            m_elementsDrawn++;
        }

        public void DrawBoxes(Box2I[] boxes, Color color, Align window = Align.TopLeft)
        {
            // TODO
            
            m_elementsDrawn++;
        }

        public void FillBox(Box2I box, Color color, Align window = Align.TopLeft)
        {
            // TODO
            
            m_elementsDrawn++;
        }

        public void FillBoxes(Box2I[] boxes, Color color, Align window = Align.TopLeft)
        {
            // TODO
            
            m_elementsDrawn++;
        }

        public void Image(string texture, Vec2I origin, Dimension? dimension = null, Align window = Align.TopLeft,
            Align image = Align.TopLeft, Align? both = null, Color? color = null, float alpha = 1)
        {
            // TODO
            
            m_elementsDrawn++;
        }

        public void Text(string text, string font, int height, Vec2I origin, Color? color = null)
        {
            // TODO
            
            m_elementsDrawn++;
        }

        public void PushVirtualDimension(Dimension dimension, ResolutionScale? scale = null)
        {
            // This peek is safe to do because we never pop the last element,
            // and there always is one on the stack.
            ResolutionScale resolutionScale = scale ?? m_resolutionStack.Peek().Scale;
            
            VirtualResolutionInfo info = new(dimension, resolutionScale);
            m_resolutionStack.Push(info);
        }

        public void PopVirtualDimension()
        {
            // We do not want to remove the base frame because that is what we
            // use when there's no virtual dimensions.
            if (m_resolutionStack.Count > 1)
                m_resolutionStack.Pop();
        }

        internal void Render(HudRenderContext context)
        {
            Precondition(m_resolutionStack.Empty(), "Forgot to pop a resolution for hud resolution");
            
            // TODO: Calculate MVP properly, use `m_elementsDrawn`.
            mat4 mvp = mat4.Identity;
            
            m_pointPrimitivePipeline.Draw(s =>
            {
                s.Mvp.Set(mvp);
            });
            
            m_linePrimitivePipeline.Draw(s =>
            {
                s.Mvp.Set(mvp);
            });
            
            m_trianglePrimitivePipeline.Draw(s =>
            {
                s.Mvp.Set(mvp);
            });
            
            m_texturePipeline.Draw(s =>
            {
                s.Mvp.Set(mvp);
            });
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
            
            m_pointPrimitivePipeline.Dispose();
            m_linePrimitivePipeline.Dispose();
            m_trianglePrimitivePipeline.Dispose();
            m_texturePipeline.Dispose();

            m_disposed = true;
        }
    }
}
