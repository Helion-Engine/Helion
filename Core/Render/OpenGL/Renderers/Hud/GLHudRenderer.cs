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
using Helion.Render.OpenGL.Primitives;
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
        private Dimension m_parentDimension;
        private VirtualResolutionInfo m_currentResolutionInfo;
        private int m_elementsDrawn;
        private bool m_disposed;

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
            m_parentDimension = context.Dimension;

            VirtualResolutionInfo info = new(context.Dimension, ResolutionScale.None, m_parentDimension);
            m_resolutionStack.Push(info);
            m_currentResolutionInfo = info;
        }

        public void Clear(Color color)
        {
            DrawBox((Vec2I.Zero, m_currentResolutionInfo.Dimension.Vector), color);
        }

        private void AddPoint(Vec2I point, ByteColor color, Align window)
        {
            (int x, int y) = m_currentResolutionInfo.Translate(point, window);
            Vec3F pos = (x, y, m_elementsDrawn);
            
            GLHudVertex vertex = new(pos, color);
            m_pointPrimitivePipeline.Vbo.Add(vertex);
        }

        public void Point(Vec2I point, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new ByteColor(color);
            AddPoint(point, byteColor, window);

            m_elementsDrawn++;
        }

        public void Points(Vec2I[] points, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new ByteColor(color);
            for (int i = 0; i < points.Length; i++)
                AddPoint(points[i], byteColor, window);
            
            m_elementsDrawn++;
        }
        
        private void AddSegment(Vec2I start, Vec2I end, ByteColor color, Align window)
        {
            Vec2I startPos = m_currentResolutionInfo.Translate(start, window);
            Vec2I endPos = m_currentResolutionInfo.Translate(end, window);
            AddSegmentPoint(startPos);
            AddSegmentPoint(endPos);

            void AddSegmentPoint(Vec2I point)
            {
                Vec3F pos = (point.X, point.Y, m_elementsDrawn);
                GLHudVertex vertex = new(pos, color);
                m_linePrimitivePipeline.Vbo.Add(vertex);
            }
        }

        public void Line(Seg2D seg, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new ByteColor(color);
            AddSegment(seg.Start.Int, seg.End.Int, byteColor, window);
            
            m_elementsDrawn++;
        }

        public void Lines(Seg2D[] segs, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new ByteColor(color);
            for (int i = 0; i < segs.Length; i++)
                AddSegment(segs[i].Start.Int, segs[i].End.Int, byteColor, window);
            
            m_elementsDrawn++;
        }
        
        private void AddBox(Box2I box, ByteColor color, Align window)
        {
            Vec2I topLeftPos = m_currentResolutionInfo.Translate(box.TopLeft, window);
            Vec2I bottomLeftPos = m_currentResolutionInfo.Translate(box.BottomLeft, window);
            Vec2I topRightPos = m_currentResolutionInfo.Translate(box.TopRight, window);
            Vec2I bottomRightPos = m_currentResolutionInfo.Translate(box.BottomRight, window);
            
            AddSegment(topLeftPos, topRightPos, color, window);
            AddSegment(topRightPos, bottomRightPos, color, window);
            AddSegment(bottomRightPos, bottomLeftPos, color, window);
            AddSegment(bottomLeftPos, topLeftPos, color, window);
        }

        public void DrawBox(Box2I box, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new ByteColor(color);
            AddBox(box, byteColor, window);
            
            m_elementsDrawn++;
        }

        public void DrawBoxes(Box2I[] boxes, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new ByteColor(color);
            for (int i = 0; i < boxes.Length; i++)
                AddBox(boxes[i], byteColor, window);
            
            m_elementsDrawn++;
        }

        private void AddTriangle(Vec2I first, Vec2I second, Vec2I third, ByteColor color)
        {
            AddTriangleVertex(first);
            AddTriangleVertex(second);
            AddTriangleVertex(third);
            
            void AddTriangleVertex(Vec2I position)
            {
                Vec3F pos = (position.X, position.Y, m_elementsDrawn);
                GLHudVertex vertex = new(pos, color);
                m_trianglePrimitivePipeline.Vbo.Add(vertex);
            }
        }

        private void AddFillBox(Box2I box, ByteColor color, Align window)
        {
            Vec2I topLeft = m_currentResolutionInfo.Translate(box.TopLeft, window);
            Vec2I bottomLeft = m_currentResolutionInfo.Translate(box.BottomLeft, window);
            Vec2I topRight = m_currentResolutionInfo.Translate(box.TopRight, window);
            Vec2I bottomRight = m_currentResolutionInfo.Translate(box.BottomRight, window);

            AddTriangle(topLeft, bottomLeft, topRight, color);
            AddTriangle(topRight, bottomLeft, bottomRight, color);
        }

        public void FillBox(Box2I box, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new ByteColor(color);
            AddFillBox(box, byteColor, window);
            
            m_elementsDrawn++;
        }

        public void FillBoxes(Box2I[] boxes, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new ByteColor(color);
            for (int i = 0; i < boxes.Length; i++)
                AddFillBox(boxes[i], byteColor, window);
            
            m_elementsDrawn++;
        }

        public void Image(string texture, Vec2I origin, Dimension? dimension = null, Align window = Align.TopLeft,
            Align image = Align.TopLeft, Align? both = null, Color? color = null, float alpha = 1.0f)
        {
            // TODO
            
            m_elementsDrawn++;
        }

        public void Text(string text, string font, int fontSize, Vec2I origin, TextAlign textAlign = TextAlign.Left, 
            Align window = Align.TopLeft, Align image = Align.TopLeft, Align? both = null, int maxWidth = int.MaxValue, 
            int maxHeight = int.MaxValue, Color? color = null, float alpha = 1.0f)
        {
            // TODO
            
            m_elementsDrawn++;
        }

        public void PushVirtualDimension(Dimension dimension, ResolutionScale? scale = null)
        {
            // This peek is safe to do because we never pop the last element,
            // and there always is one on the stack.
            ResolutionScale resolutionScale = scale ?? m_resolutionStack.Peek().Scale;
            
            VirtualResolutionInfo info = new(dimension, resolutionScale, m_parentDimension);
            m_resolutionStack.Push(info);
            m_currentResolutionInfo = info;
        }

        public void PopVirtualDimension()
        {
            // We do not want to remove the base frame because that is what we
            // use when there's no virtual dimensions.
            if (m_resolutionStack.Count <= 1) 
                return;
            
            m_resolutionStack.Pop();
            m_currentResolutionInfo = m_resolutionStack.Peek();
        }

        private static mat4 CreateMvp(HudRenderContext context)
        {
            (int w, int h) = context.Dimension;
            
            // TODO: Properly handle near/far
            mat4 mvp = mat4.Ortho(0, w, h, 0, 0, 1);
            
            return mvp;
        }

        internal void Render(HudRenderContext context)
        {
            Precondition(m_resolutionStack.Empty(), "Forgot to pop a resolution for hud resolution");
            
            mat4 mvp = CreateMvp(context);
            
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
