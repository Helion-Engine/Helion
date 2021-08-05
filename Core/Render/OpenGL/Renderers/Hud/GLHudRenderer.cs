using System;
using System.Collections.Generic;
using System.Drawing;
using GlmSharp;
using Helion.Geometry;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Graphics.String;
using Helion.Render.Common;
using Helion.Render.Common.Context;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Pipeline;
using Helion.Render.OpenGL.Primitives;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Util;
using Helion.Resources;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Hud
{
    /// <summary>
    /// A renderer for the hud which uses GL backing data.
    /// </summary>
    public class GLHudRenderer : IHudRenderContext
    {
        private readonly GLRenderer m_renderer;
        private readonly GLTextureManager m_textureManager;
        private readonly RenderPipeline<GLHudPrimitiveShader, GLHudPrimitiveVertex> m_pointPrimitivePipeline;
        private readonly RenderPipeline<GLHudPrimitiveShader, GLHudPrimitiveVertex> m_linePrimitivePipeline;
        private readonly RenderPipeline<GLHudPrimitiveShader, GLHudPrimitiveVertex> m_trianglePrimitivePipeline;
        private readonly RenderTexturePipeline<GLHudTextureShader, GLHudTextureVertex> m_texturePipeline;
        private readonly Stack<VirtualResolutionInfo> m_resolutionStack = new();
        private Dimension m_parentDimension = (800, 600);
        private VirtualResolutionInfo m_currentResolutionInfo = new((800, 600), ResolutionScale.None, (800, 600));
        private int m_elementsDrawn;
        private bool m_disposed;

        public Dimension Dimension => m_currentResolutionInfo.Dimension;
        public Dimension WindowDimension => m_renderer.Window.Dimension;
        public IRendererTextureManager Textures => m_textureManager;

        public GLHudRenderer(GLRenderer renderer, GLTextureManager textureManager)
        {
            m_renderer = renderer;
            m_textureManager = textureManager;
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

        public void Clear(Color color, float alpha = 1.0f)
        {
            Color drawColor = Color.FromArgb((int)(alpha / 255.0f), color.R, color.G, color.B);
            DrawBox((Vec2I.Zero, m_currentResolutionInfo.Dimension.Vector), drawColor);
        }

        private void AddPoint(Vec2I point, ByteColor color, Align window)
        {
            (int x, int y) = m_currentResolutionInfo.Translate(point, window);
            Vec3F pos = (x, y, m_elementsDrawn);
            
            GLHudPrimitiveVertex vertex = new(pos, color);
            m_pointPrimitivePipeline.Vbo.Add(vertex);
        }

        public void Point(Vec2I point, Color color, Align window = Align.TopLeft, float alpha = 1.0f)
        {
            ByteColor byteColor = new ByteColor(color, alpha);
            AddPoint(point, byteColor, window);

            m_elementsDrawn++;
        }

        public void Points(Vec2I[] points, Color color, Align window = Align.TopLeft, float alpha = 1.0f)
        {
            ByteColor byteColor = new ByteColor(color, alpha);
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
                GLHudPrimitiveVertex vertex = new(pos, color);
                m_linePrimitivePipeline.Vbo.Add(vertex);
            }
        }

        public void Line(Seg2D seg, Color color, Align window = Align.TopLeft, float alpha = 1.0f)
        {
            ByteColor byteColor = new ByteColor(color, alpha);
            AddSegment(seg.Start.Int, seg.End.Int, byteColor, window);
            
            m_elementsDrawn++;
        }

        public void Lines(Seg2D[] segs, Color color, Align window = Align.TopLeft, float alpha = 1.0f)
        {
            ByteColor byteColor = new ByteColor(color, alpha);
            for (int i = 0; i < segs.Length; i++)
                AddSegment(segs[i].Start.Int, segs[i].End.Int, byteColor, window);
            
            m_elementsDrawn++;
        }
        
        private void AddBox(HudBox box, ByteColor color, Align window, Align anchor)
        {
            Dimension dimension = box.Dimension;
            
            Vec2I topLeft = anchor.Translate(box.TopLeft, dimension);
            Vec2I bottomLeft = anchor.Translate(box.BottomLeft, dimension);
            Vec2I topRight = anchor.Translate(box.TopRight, dimension);
            Vec2I bottomRight = anchor.Translate(box.TopLeft, dimension);
            
            topLeft = m_currentResolutionInfo.Translate(topLeft, window);
            bottomLeft = m_currentResolutionInfo.Translate(bottomLeft, window);
            topRight = m_currentResolutionInfo.Translate(topRight, window);
            bottomRight = m_currentResolutionInfo.Translate(bottomRight, window);
            
            AddSegment(topLeft, topRight, color, window);
            AddSegment(topRight, bottomRight, color, window);
            AddSegment(bottomRight, bottomLeft, color, window);
            AddSegment(bottomLeft, topLeft, color, window);
        }

        public void DrawBox(HudBox box, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft, 
            float alpha = 1.0f)
        {
            ByteColor byteColor = new ByteColor(color, alpha);
            AddBox(box, byteColor, window, anchor);
            
            m_elementsDrawn++;
        }

        public void DrawBoxes(HudBox[] boxes, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft, 
            float alpha = 1.0f)
        {
            ByteColor byteColor = new ByteColor(color, alpha);
            for (int i = 0; i < boxes.Length; i++)
                AddBox(boxes[i], byteColor, window, anchor);
            
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
                GLHudPrimitiveVertex vertex = new(pos, color);
                m_trianglePrimitivePipeline.Vbo.Add(vertex);
            }
        }

        private void AddFillBox(HudBox box, ByteColor color, Align window, Align anchor)
        {
            Dimension dimension = box.Dimension;
            
            Vec2I topLeft = anchor.Translate(box.TopLeft, dimension);
            Vec2I bottomLeft = anchor.Translate(box.BottomLeft, dimension);
            Vec2I topRight = anchor.Translate(box.TopRight, dimension);
            Vec2I bottomRight = anchor.Translate(box.TopLeft, dimension);
            
            topLeft = m_currentResolutionInfo.Translate(topLeft, window);
            bottomLeft = m_currentResolutionInfo.Translate(bottomLeft, window);
            topRight = m_currentResolutionInfo.Translate(topRight, window);
            bottomRight = m_currentResolutionInfo.Translate(bottomRight, window);

            AddTriangle(topLeft, bottomLeft, topRight, color);
            AddTriangle(topRight, bottomLeft, bottomRight, color);
        }

        public void FillBox(HudBox box, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft, 
            float alpha = 1.0f)
        {
            ByteColor byteColor = new ByteColor(color, alpha);
            AddFillBox(box, byteColor, window, anchor);
            
            m_elementsDrawn++;
        }

        public void FillBoxes(HudBox[] boxes, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft, 
            float alpha = 1.0f)
        {
            ByteColor byteColor = new ByteColor(color, alpha);
            for (int i = 0; i < boxes.Length; i++)
                AddFillBox(boxes[i], byteColor, window, anchor);
            
            m_elementsDrawn++;
        }

        public void Image(string texture, HudBox area, out HudBox drawArea, Align window = Align.TopLeft, 
            Align anchor = Align.TopLeft, Align? both = null, ResourceNamespace resourceNamespace = ResourceNamespace.Global,
            Color? color = null, float scale = 1.0f, float alpha = 1.0f)
        {
            Image(texture, out drawArea, area, null, window, anchor, both, resourceNamespace, color, scale, alpha);
        }

        public void Image(string texture, Vec2I origin, out HudBox drawArea, Align window = Align.TopLeft,
            Align anchor = Align.TopLeft, Align? both = null, ResourceNamespace resourceNamespace = ResourceNamespace.Global,
            Color? color = null, float scale = 1.0f, float alpha = 1.0f)
        {
            Image(texture, out drawArea, null, origin, window, anchor, both, resourceNamespace, color, scale, alpha);
        }

        private void Image(string texture, out HudBox drawArea, HudBox? area = null, Vec2I? origin = null, 
            Align window = Align.TopLeft, Align anchor = Align.TopLeft, Align? both = null, 
            ResourceNamespace resourceNamespace = ResourceNamespace.Global, Color? color = null, 
            float scale = 1.0f, float alpha = 1.0f)
        {
            Precondition(area != null || origin != null, "Did not specify an area or origin when drawing a hud image");
            
            GLTextureHandle handle = m_textureManager.Get(texture, resourceNamespace);

            Vec2I topLeft = origin ?? Vec2I.Zero;
            if (area != null)
                topLeft = area.Value.TopLeft;

            Dimension dimension = handle.Dimension;
            if (area != null)
                dimension = area.Value.Dimension;
            
            dimension.Scale(scale);

            window = both ?? window;
            anchor = both ?? anchor;
            topLeft = anchor.Translate(topLeft, dimension);
            topLeft = m_currentResolutionInfo.Translate(topLeft, window);

            Vec2I topRight = topLeft + (dimension.Width, 0);
            Vec2I bottomLeft = topLeft + (0, dimension.Height);
            Vec2I bottomRight = topLeft + dimension;
            drawArea = (topLeft, bottomRight);

            ByteColor byteColor = new(color ?? Color.White);
            GLHudTextureVertex quadTL = new(topLeft.Float.To3D(m_elementsDrawn), (0.0f, 0.0f), byteColor, alpha);
            GLHudTextureVertex quadTR = new(topRight.Float.To3D(m_elementsDrawn), (1.0f, 0.0f), byteColor, alpha);
            GLHudTextureVertex quadBL = new(bottomLeft.Float.To3D(m_elementsDrawn), (0.0f, 1.0f), byteColor, alpha);
            GLHudTextureVertex quadBR = new(bottomRight.Float.To3D(m_elementsDrawn), (1.0f, 1.0f), byteColor, alpha);
            m_texturePipeline.Quad(handle.Texture, quadTL, quadTR, quadBL, quadBR);
            
            m_elementsDrawn++;
        }

        public void Text(ColoredString text, string font, int fontSize, Vec2I origin, out Dimension drawArea,
            TextAlign textAlign = TextAlign.Left, Align window = Align.TopLeft, Align anchor = Align.TopLeft,
            Align? both = null, int maxWidth = int.MaxValue, int maxHeight = int.MaxValue, float scale = 1.0f,
            float alpha = 1.0f)
        {
            drawArea = default;
            
            // TODO
            
            m_elementsDrawn++;
        }

        public void Text(string text, string font, int fontSize, Vec2I origin, out Dimension drawArea, 
            TextAlign textAlign = TextAlign.Left, Align window = Align.TopLeft, Align anchor = Align.TopLeft, 
            Align? both = null, int maxWidth = int.MaxValue, int maxHeight = int.MaxValue, Color? color = null, 
            float scale = 1.0f, float alpha = 1.0f)
        {
            drawArea = default;
            
            // TODO
            
            m_elementsDrawn++;
        }

        public Dimension MeasureText(string text, string font, int fontSize, int maxWidth = int.MaxValue,
            int maxHeight = int.MaxValue, float scale = 1.0f)
        {
            // TODO
            return default;
        }

        public void PushVirtualDimension(Dimension dimension, ResolutionScale? scale = null,
            float? aspectRatio = null)
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

        private mat4 CreateMvp(HudRenderContext context)
        {
            // There's a few things we do here:
            //
            // 1) We draw from the top downwards because we have the top left
            // being our draw origin, and thus they are inverted.
            //
            // 2) We flip the Z depths so that we draw back-to-front, meaning
            // the stuff we drew first should be drawn behind the stuff we drew
            // later on. This gives us the Painters Algorithm approach we want.
            (int w, int h) = context.Dimension;
            return mat4.Ortho(0, w, h, 0, -(m_elementsDrawn + 1), 0);
        }

        internal void Render(HudRenderContext context)
        {
            Precondition(m_resolutionStack.Count <= 1, "Forgot to pop a resolution for hud resolution");
            
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
                GL.ActiveTexture(TextureUnit.Texture0);
                s.Mvp.Set(mvp);
                s.Tex.Set(0);
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
