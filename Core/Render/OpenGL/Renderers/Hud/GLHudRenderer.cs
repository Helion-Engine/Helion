using System;
using System.Collections.Generic;
using System.Drawing;
using GlmSharp;
using Helion.Geometry;
using Helion.Geometry.Boxes;
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
using Helion.Render.OpenGL.Renderers.Hud.Text;
using Helion.Render.OpenGL.Textures;
using Helion.Resources;
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
        private readonly GLRenderer m_renderer;
        private readonly GLTextureManager m_textureManager;
        private readonly RenderPipeline<GLHudPrimitiveShader, GLHudPrimitiveVertex> m_pointPrimitivePipeline;
        private readonly RenderPipeline<GLHudPrimitiveShader, GLHudPrimitiveVertex> m_linePrimitivePipeline;
        private readonly RenderPipeline<GLHudPrimitiveShader, GLHudPrimitiveVertex> m_trianglePrimitivePipeline;
        private readonly RenderTexturePipeline<GLHudTextureShader, GLHudTextureVertex> m_texturePipeline;
        private readonly Stack<VirtualResolutionInfo> m_resolutionStack = new();
        private readonly GLHudTextHelper m_hudTextHelper = new();
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
        
        private bool PointOutsideBottomRightViewport(Vec2I point)
        {
            return point.X > m_parentDimension.Width || point.Y > m_parentDimension.Height;
        }
        
        private Vec2I CalculateDrawPoint(Vec2I point, Align window, Align anchor)
        {
            // TODO: Not optimal, only want to calculate it for a point.
            HudBox temp = (point, point);
            HudBox virtualBox = m_currentResolutionInfo.VirtualTranslate(temp, window, anchor);
            HudBox parentBox = m_currentResolutionInfo.VirtualToParent(virtualBox);
            return parentBox.TopLeft;
        }

        private HudBox CalculateDrawArea(HudBox box, Align window, Align anchor)
        {
            HudBox virtualBox = m_currentResolutionInfo.VirtualTranslate(box, window, anchor);
            HudBox parentBox = m_currentResolutionInfo.VirtualToParent(virtualBox);
            return parentBox;
        }
        
        private HudBox CalculateDrawArea(HudBox box, Align window, Align anchor, out HudBox virtualBox)
        {
            virtualBox = m_currentResolutionInfo.VirtualTranslate(box, window, anchor);
            HudBox parentBox = m_currentResolutionInfo.VirtualToParent(virtualBox);
            return parentBox;
        }

        internal void Begin(HudRenderContext context)
        {
            m_elementsDrawn = 0;
            m_resolutionStack.Clear();
            m_parentDimension = context.Dimension;
            m_hudTextHelper.Reset();

            VirtualResolutionInfo info = new(context.Dimension, ResolutionScale.None, m_parentDimension);
            m_resolutionStack.Push(info);
            m_currentResolutionInfo = info;
        }

        public void Clear(Color color, float alpha = 1.0f)
        {
            HudBox area = (Vec2I.Zero, m_currentResolutionInfo.Dimension.Vector);
            FillBox(area, color, alpha: alpha);
        }

        private void AddPoint(Vec2I point, ByteColor color, Align window)
        {
            HudBox box = CalculateDrawArea((point, point + (1, 1)), window, Align.TopLeft);
            (int x, int y) = box.TopLeft;
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
            Vec2I startPos = CalculateDrawPoint(start, window, Align.TopLeft);
            Vec2I endPos = CalculateDrawPoint(end, window, Align.TopLeft);
            
            AddSegmentPoint(startPos);
            AddSegmentPoint(endPos);

            void AddSegmentPoint(Vec2I point)
            {
                Vec3F pos = (point.X, point.Y, m_elementsDrawn);
                GLHudPrimitiveVertex vertex = new(pos, color);
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
        
        private void AddBox(HudBox box, ByteColor color, Align window, Align anchor)
        {
            if (PointOutsideBottomRightViewport(box.TopLeft))
                return;

            HudBox area = CalculateDrawArea(box, window, anchor);
            
            AddSegment(area.TopLeft, area.TopRight, color, window);
            AddSegment(area.TopRight, area.BottomRight, color, window);
            AddSegment(area.BottomRight, area.BottomLeft, color, window);
            AddSegment(area.BottomLeft, area.TopLeft, color, window);
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
            if (PointOutsideBottomRightViewport(box.TopLeft))
                return;
            
            HudBox area = CalculateDrawArea(box, window, anchor);

            AddTriangle(area.TopLeft, area.BottomLeft, area.TopRight, color);
            AddTriangle(area.TopRight, area.BottomLeft, area.BottomRight, color);
        }

        public void FillBox(HudBox box, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft, 
            float alpha = 1.0f)
        {
            if (alpha.ApproxEquals(1.0f))
            {
                ByteColor byteColor = new ByteColor(color, alpha);
                AddFillBox(box, byteColor, window, anchor);
            
                m_elementsDrawn++;
            }
            else
            {
                Image("", out _, box, color: color, alpha: alpha, overrideHandle: m_textureManager.WhiteHandle);
            }
        }

        public void FillBoxes(HudBox[] boxes, Color color, Align window = Align.TopLeft, Align anchor = Align.TopLeft, 
            float alpha = 1.0f)
        {
            bool callImage = !alpha.ApproxEquals(1.0f);

            if (callImage)
            {
                ByteColor byteColor = new ByteColor(color, alpha);
                for (int i = 0; i < boxes.Length; i++)
                    AddFillBox(boxes[i], byteColor, window, anchor);
            }
            else
            {
                for (int i = 0; i < boxes.Length; i++)
                    Image("", out _, boxes[i], color: color, alpha: alpha, overrideHandle: m_textureManager.WhiteHandle);
            }

            if (callImage)
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
            float scale = 1.0f, float alpha = 1.0f, GLTextureHandle? overrideHandle = null)
        {
            Precondition(area != null || origin != null, "Did not specify an area or origin when drawing a hud image");
            
            GLTextureHandle handle = overrideHandle ?? m_textureManager.Get(texture, resourceNamespace);
            
            Vec2I newOrigin = origin ?? Vec2I.Zero;
            if (area != null)
                newOrigin = area.Value.TopLeft;

            Dimension dimension = handle.Dimension;
            if (area != null)
                dimension = area.Value.Dimension;
            
            dimension.Scale(scale);

            window = both ?? window;
            anchor = both ?? anchor;
            
            HudBox renderArea = (newOrigin, newOrigin + dimension);
            renderArea = CalculateDrawArea(renderArea, window, anchor, out drawArea);
            
            if (PointOutsideBottomRightViewport(renderArea.TopLeft))
                return;

            ByteColor byteColor = new(color ?? Color.White);
            Vec3F topLeft = renderArea.TopLeft.Float.To3D(m_elementsDrawn);
            Vec3F topRight = renderArea.TopRight.Float.To3D(m_elementsDrawn);
            Vec3F bottomLeft = renderArea.BottomLeft.Float.To3D(m_elementsDrawn);
            Vec3F bottomRight = renderArea.BottomRight.Float.To3D(m_elementsDrawn);

            // Note: Because UV's are inverted, we use the flipped version of the
            // coordinates (so TopLeft <=> BottomLeft, and TopRight <=> BottomRight).
            GLHudTextureVertex quadTL = new(topLeft, handle.UV.BottomLeft, byteColor, alpha);
            GLHudTextureVertex quadTR = new(topRight, handle.UV.BottomRight, byteColor, alpha);
            GLHudTextureVertex quadBL = new(bottomLeft, handle.UV.TopLeft, byteColor, alpha);
            GLHudTextureVertex quadBR = new(bottomRight, handle.UV.TopRight, byteColor, alpha);
            m_texturePipeline.Quad(handle.Texture, quadTL, quadTR, quadBL, quadBR);
            
            m_elementsDrawn++;
        }

        public void Text(ColoredString text, string font, int fontSize, Vec2I origin, out Dimension drawArea,
            TextAlign textAlign = TextAlign.Left, Align window = Align.TopLeft, Align anchor = Align.TopLeft,
            Align? both = null, int maxWidth = int.MaxValue, int maxHeight = int.MaxValue, float scale = 1.0f,
            float alpha = 1.0f)
        {
            drawArea = default;
            
            if (text.Length == 0 || !m_textureManager.TryGetFont(font, out GLFontTexture fontHandle))
                return;
            
            ReadOnlySpan<RenderableCharacter> chars = m_hudTextHelper.Calculate(text, fontHandle, fontSize, 
                textAlign, maxWidth, maxHeight, scale, out drawArea);

            window = both ?? window;
            anchor = both ?? anchor;
            Vec2I topLeft = CalculateDrawPoint(origin, window, anchor);
            
            if (PointOutsideBottomRightViewport(topLeft))
                return;

            for (int i = 0; i < chars.Length; i++)
            {
                ByteColor byteColor = new(text[i].Color);
                AddTextCharacter(topLeft, alpha, chars[i], byteColor, fontHandle);
            }

            m_elementsDrawn++;
        }

        public void Text(string text, string font, int fontSize, Vec2I origin, out Dimension drawArea, 
            TextAlign textAlign = TextAlign.Left, Align window = Align.TopLeft, Align anchor = Align.TopLeft, 
            Align? both = null, int maxWidth = int.MaxValue, int maxHeight = int.MaxValue, Color? color = null, 
            float scale = 1.0f, float alpha = 1.0f)
        {
            drawArea = default;

            if (text.Length == 0 || !m_textureManager.TryGetFont(font, out GLFontTexture fontHandle))
                return;

            ReadOnlySpan<RenderableCharacter> chars = m_hudTextHelper.Calculate(text, fontHandle, fontSize, 
                textAlign, maxWidth, maxHeight, scale, out drawArea);
            
            window = both ?? window;
            anchor = both ?? anchor;
            Vec2I topLeft = CalculateDrawPoint(origin, window, anchor);
            
            if (PointOutsideBottomRightViewport(topLeft))
                return;

            ByteColor byteColor = new(color ?? Color.White);
            for (int i = 0; i < chars.Length; i++)
                AddTextCharacter(topLeft, alpha, chars[i], byteColor, fontHandle);

            m_elementsDrawn++;
        }

        private void AddTextCharacter(Vec2I origin, float alpha, RenderableCharacter c, ByteColor byteColor,
            GLFontTexture fontHandle)
        {
            HudBox area = c.Area + origin;
            Box2F uv = c.UV;
            
            // Note: Because UV's are inverted, we use the flipped version of the
            // coordinates (so TopLeft <=> BottomLeft, and TopRight <=> BottomRight).
            GLHudTextureVertex quadTL = new(area.TopLeft.Float.To3D(m_elementsDrawn), uv.BottomLeft, byteColor, alpha);
            GLHudTextureVertex quadTR = new(area.TopRight.Float.To3D(m_elementsDrawn), uv.BottomRight, byteColor, alpha);
            GLHudTextureVertex quadBL = new(area.BottomLeft.Float.To3D(m_elementsDrawn), uv.TopLeft, byteColor, alpha);
            GLHudTextureVertex quadBR = new(area.BottomRight.Float.To3D(m_elementsDrawn), uv.TopRight, byteColor, alpha);
            
            m_texturePipeline.Quad(fontHandle, quadTL, quadTR, quadBL, quadBR);
        }

        public Dimension MeasureText(string text, string font, int fontSize, int maxWidth = int.MaxValue,
            int maxHeight = int.MaxValue, float scale = 1.0f)
        {
            if (!m_textureManager.TryGetFont(font, out GLFontTexture fontHandle))
                return (0, 0);
            
            m_hudTextHelper.Calculate(text, fontHandle, fontSize, TextAlign.Left, maxWidth, maxHeight, scale, 
                out Dimension drawArea);

            return drawArea;
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
            //
            // 3) Because we draw at index 0, we don't want the first drawn thing
            // to be sitting on the far plane, so instead of using zFar = 0, we use
            // zFar = 1. This way the range of [0, elementsDrawn] will fit in the
            // depth defined by [-1, elementsDrawn + 1].
            (int w, int h) = context.Dimension;
            return mat4.Ortho(0, w, h, 0, -(m_elementsDrawn + 1), 1);
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
                s.Tex.Set(TextureUnit.Texture0);
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
