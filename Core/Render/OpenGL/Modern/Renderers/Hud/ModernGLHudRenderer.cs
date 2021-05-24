using System;
using System.Collections.Generic;
using System.Drawing;
using GlmSharp;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Framebuffer;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Modern.Framebuffers;
using Helion.Render.OpenGL.Modern.Renderers.Hud.Framebuffers;
using Helion.Render.OpenGL.Modern.Renderers.Hud.Primitives;
using Helion.Render.OpenGL.Modern.Renderers.Hud.Textures;
using Helion.Render.OpenGL.Modern.Textures;
using Helion.Render.OpenGL.Pipeline;
using Helion.Render.OpenGL.Primitives;
using Helion.Util.Extensions;
using OpenTK.Graphics.OpenGL4;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Modern.Renderers.Hud
{
    public class ModernGLHudRenderer : IHudRenderer
    {
        private readonly IFramebuffer m_framebuffer;
        private readonly ModernGLTextureManager m_textureManager;
        private readonly PrimitiveRenderPipeline m_pointsPipeline;
        private readonly PrimitiveRenderPipeline m_linesPipeline;
        private readonly PrimitiveRenderPipeline m_shapesPipeline;
        private readonly RenderPipeline<HudTextureShader, HudTextureVertex> m_texturePipeline;
        private readonly RenderTexturePipeline<HudFramebufferShader, HudFramebufferVertex> m_framebufferTexturePipeline;
        private readonly Stack<RenderDimensions> m_renderDimensions = new();
        private int m_depthIndex;
        private bool m_disposed;

        public ModernGLHudRenderer(IFramebuffer framebuffer, ModernGLTextureManager textureManager)
        {
            m_framebuffer = framebuffer;
            m_textureManager = textureManager;
            m_pointsPipeline = new($"[{framebuffer.Name}] HUD point renderer", PrimitiveType.Points);
            m_linesPipeline = new($"[{framebuffer.Name}] HUD line renderer", PrimitiveType.Lines);
            m_shapesPipeline = new($"[{framebuffer.Name}] HUD shape renderer", PrimitiveType.Triangles);
            m_texturePipeline = new($"[{framebuffer.Name}] HUD texture renderer", BufferUsageHint.StreamDraw, PrimitiveType.Triangles);
            m_framebufferTexturePipeline = new($"[{framebuffer.Name}] HUD framebuffer renderer");
        }

        ~ModernGLHudRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        /// <summary>
        /// Translates a point to the proper location of the viewport based on
        /// the viewport, and any virtual dimensions that were pushed.
        /// </summary>
        /// <param name="point">The point to translate.</param>
        /// <param name="align">The alignment to do.</param>
        /// <returns></returns>
        private Vec2I Translate(Vec2I point, Align align)
        {
            // If there is no virtual dimension, then our point only needs to
            // be translated relative to the parent viewport.
            if (!m_renderDimensions.TryPeek(out RenderDimensions renderDim))
                return align.Translate(point, m_framebuffer.Dimension);
            
            // Otherwise, translate it relative to the virtual viewport, and
            // then scale/adjust that point from the virtual viewport into the
            // parent viewport.
            point = align.Translate(point, renderDim);
            return renderDim.Translate(point, m_framebuffer.Dimension);
        }
        
        private Box2I Translate(Vec2I point, Dimension dimension, Align align)
        {
            // There's probably some extra math in here we don't need to do.
            return (Translate(point, align), Translate(point + dimension.Vector, align));
        }
        
        private Box2I Translate(Box2I box, Align align)
        {
            // There's probably some extra math in here we don't need to do.
            return (Translate(box.Min, align), Translate(box.Max, align));
        }

        private Dimension GetViewport()
        {
            return m_renderDimensions.TryPeek(out RenderDimensions dim) ? dim : m_framebuffer.Dimension;
        }

        public void Clear(Color color)
        {
            Box2I viewport = ((0, 0), GetViewport().Vector);
            FillBox(viewport, color);
        }

        private void AddPoint(Vec2I point, ByteColor color, Align window)
        {
            Vec3F position = Translate(point, window).Float.To3D(m_depthIndex);
            HudPrimitiveVertex vertex = new(position, color);
            m_pointsPipeline.Vbo.Add(vertex);
        }

        public void Point(Vec2I point, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new(color);
            AddPoint(point, byteColor, window);

            m_depthIndex++;
        }

        public void Points(Vec2I[] points, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new(color);

            for (int i = 0; i < points.Length; i++)
                AddPoint(points[i], byteColor, window);

            m_depthIndex++;
        }

        private void AddLine(Seg2D seg, ByteColor color, Align window)
        {
            Vec3F startPos = Translate(seg.Start.Int, window).Float.To3D(m_depthIndex);
            Vec3F endPos = Translate(seg.End.Int, window).Float.To3D(m_depthIndex);

            HudPrimitiveVertex start = new(startPos, color);
            HudPrimitiveVertex end = new(endPos, color);
            m_linesPipeline.Vbo.Add(start);
            m_linesPipeline.Vbo.Add(end);
        }

        public void Line(Seg2D seg, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new(color);
            AddLine(seg, byteColor, window);

            m_depthIndex++;
        }

        public void Lines(Seg2D[] segs, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new(color);

            for (int i = 0; i < segs.Length; i++)
                AddLine(segs[i], byteColor, window);

            m_depthIndex++;
        }

        public void DrawBox(Box2I box, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new(color);

            var ((left, bottom), (right, top)) = box;
            Seg2D leftEdge = ((left, bottom), (left, top));
            Seg2D topEdge = ((left, top), (right, top));
            Seg2D rightEdge = ((right, top), (right, bottom));
            Seg2D bottomEdge = ((right, bottom), (left, bottom));

            // We do it this way to avoid allocating an array.
            AddLine(leftEdge, byteColor, window);
            AddLine(topEdge, byteColor, window);
            AddLine(rightEdge, byteColor, window);
            AddLine(bottomEdge, byteColor, window);
        }

        public void DrawBoxes(Box2I[] boxes, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new(color);

            for (int i = 0; i < boxes.Length; i++)
            {
                var ((left, bottom), (right, top)) = boxes[i];
                Seg2D leftEdge = ((left, bottom), (left, top));
                Seg2D topEdge = ((left, top), (right, top));
                Seg2D rightEdge = ((right, top), (right, bottom));
                Seg2D bottomEdge = ((right, bottom), (left, bottom));

                // We do it this way to avoid allocating an array.
                AddLine(leftEdge, byteColor, window);
                AddLine(topEdge, byteColor, window);
                AddLine(rightEdge, byteColor, window);
                AddLine(bottomEdge, byteColor, window);
            }
        }

        private void AddBox(Box2I box, ByteColor color, Align window)
        {
            (Vec2I min, Vec2I max) = Translate(box, window);
            float z = m_depthIndex;
            HudPrimitiveVertex topLeft = new((min.X, min.Y, z), color);
            HudPrimitiveVertex topRight = new((max.X, min.Y, z), color);
            HudPrimitiveVertex bottomLeft = new((min.X, max.Y, z), color);
            HudPrimitiveVertex bottomRight = new((max.X, max.Y, z), color);

            AddTriangle(m_shapesPipeline.Vbo, topLeft, bottomLeft, bottomRight);
            AddTriangle(m_shapesPipeline.Vbo, topLeft, bottomRight, topRight);

            m_depthIndex++;

            static void AddTriangle(VertexBufferObject<HudPrimitiveVertex> vbo, HudPrimitiveVertex first,
                HudPrimitiveVertex second, HudPrimitiveVertex third)
            {
                vbo.Add(first);
                vbo.Add(second);
                vbo.Add(third);
            }

            m_depthIndex++;
        }

        public void FillBox(Box2I box, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new(color);

            AddBox(box, byteColor, window);
        }

        public void FillBoxes(Box2I[] boxes, Color color, Align window = Align.TopLeft)
        {
            ByteColor byteColor = new(color);

            for (int i = 0; i < boxes.Length; i++)
                AddBox(boxes[i], byteColor, window);

            m_depthIndex++;
        }

        public void Image(string textureName, Vec2I origin, Dimension? dimension = null, Align window = Align.TopLeft,
            Align image = Align.TopLeft, Align? both = null, Color? color = null, float alpha = 1.0f)
        {
            Align windowAlign = both ?? window;
            Align imageAlign = both ?? image;
            ModernGLTexture texture = m_textureManager.NullTexture;
            Dimension sides = dimension ?? texture.Image.Dimension;
            Vec2I imageOffset = imageAlign.Translate(Vec2I.Zero, sides);

            (Vec2I min, Vec2I max) = Translate(origin + imageOffset, GetViewport(), windowAlign);
            float z = m_depthIndex;
            ByteColor byteColor = new(color ?? Color.White);
            BindlessHandle handle = new(texture.Handle);

            HudTextureVertex topLeft = MakeVertex(min.X, min.Y, z, 0, 0, byteColor, alpha, handle);
            HudTextureVertex topRight = MakeVertex(max.X, min.Y, z, 1, 0, byteColor, alpha, handle);
            HudTextureVertex bottomLeft = MakeVertex(min.X, max.Y, z, 0, 1, byteColor, alpha, handle);
            HudTextureVertex bottomRight = MakeVertex(max.X, max.Y, z, 1, 1, byteColor, alpha, handle);

            AddTriangle(m_texturePipeline.Vbo, topLeft, bottomLeft, bottomRight);
            AddTriangle(m_texturePipeline.Vbo, topLeft, bottomRight, topRight);

            m_depthIndex++;

            static HudTextureVertex MakeVertex(float x, float y, float z, float u, float v, ByteColor color,
                float alpha, BindlessHandle handle)
            {
                vec3 pos = new(x, y, z);
                vec2 uv = new(u, v);
                return new HudTextureVertex(pos, uv, color, alpha, handle);
            }

            static void AddTriangle(VertexBufferObject<HudTextureVertex> vbo, HudTextureVertex first,
                HudTextureVertex second, HudTextureVertex third)
            {
                vbo.Add(first);
                vbo.Add(second);
                vbo.Add(third);
            }
        }

        public void Text(string text, string font, int height, Vec2I origin, Color? color = null)
        {
            // TODO

            m_depthIndex++;
        }

        public void FrameBuffer(IFramebuffer framebuffer, Vec2I origin, Dimension? dimension = null,
            Align window = Align.TopLeft,
            Align image = Align.TopLeft, Align? both = null, Color? color = null, float alpha = 1.0f)
        {
            if (framebuffer is not ModernGLFramebuffer glFrameBuffer)
                throw new Exception($"Framebuffer is not the expected type (expecting modern OpenGL, got {framebuffer.GetType().Name})");

            Align windowAlign = both ?? window;
            Align imageAlign = both ?? image;
            Dimension sides = dimension ?? glFrameBuffer.Dimension;
            Vec2I imageOffset = imageAlign.Translate(Vec2I.Zero, sides);

            (Vec2I min, Vec2I max) = Translate(origin + imageOffset, GetViewport(), windowAlign);
            float z = m_depthIndex;

            HudFramebufferVertex topLeft = MakeVertex(min.X, min.Y, z, 0, 0);
            HudFramebufferVertex topRight = MakeVertex(max.X, min.Y, z, 1, 0);
            HudFramebufferVertex bottomLeft = MakeVertex(min.X, max.Y, z, 0, 1);
            HudFramebufferVertex bottomRight = MakeVertex(max.X, max.Y, z, 1, 1);

            m_framebufferTexturePipeline.Quad(glFrameBuffer.Texture, topLeft, topRight, bottomLeft, bottomRight);

            m_depthIndex++;

            static HudFramebufferVertex MakeVertex(float x, float y, float z, float u, float v)
            {
                vec3 pos = new(x, y, z);
                vec2 uv = new(u, v);
                return new HudFramebufferVertex(pos, uv);
            }
        }

        public void PushVirtualDimension(Dimension dimension, ResolutionScale? scale = null)
        {
            Precondition(m_renderDimensions.Count < 1000, "Forgetting to pop and clear GL hud renderer");

            ResolutionScale resScale = scale ?? (m_renderDimensions.TryPeek(out RenderDimensions renderDim) ? 
                                                    renderDim.ScaleType : 
                                                    ResolutionScale.None);
            
            RenderDimensions renderDimensions = new(dimension, resScale);
            m_renderDimensions.Push(renderDimensions);
        }

        public void PopVirtualDimension()
        {
            Precondition(m_renderDimensions.Count > 0, "Calling GL hud renderer virtual dimension pop too many times");

            m_renderDimensions.TryPop(out _);
        }

        private mat4 CreateMvp(Dimension viewport)
        {
            // Note: The origin is intended to be at the top left corner.
            (int width, int height) = viewport;
            mat4 mvp = mat4.Ortho(0, width, height, 0, -1, 1);
            
            // GLM's ortho is fighting me on something simple, so instead I'll
            // make it do what I want with some quick tweaks.
            //
            // In short, map the range [d, 0] onto [-1/2, 1/2], whereby the last
            // drawn element (which will have depth d - 1 since we increment d
            // always after drawing) should be drawn closest to the near plane.
            //
            // To do this, we can start drawing at the far plane of 1/2, and then
            // move closer by 1/d. We then add 1 to the denominator to prevent
            // divide-by-zero errors. This maps it onto [-1/2, 1/2] approximately.
            mvp.m22 = -1.0f / (m_depthIndex + 1);
            mvp.m32 = 0.5f;

            return mvp;
        }

        public void Render(Dimension viewport)
        {
            if (m_disposed)
                return;
            
            Precondition(m_renderDimensions.Empty(), "Calling GL hud renderer virtual dimension pop too many times");

            mat4 mvp = CreateMvp(viewport);
            
            m_pointsPipeline.Draw(s =>
            {
                s.Mvp.Set(mvp);
            });
            
            m_linesPipeline.Draw(s =>
            {
                s.Mvp.Set(mvp);
            });
            
            m_shapesPipeline.Draw(s =>
            {
                s.Mvp.Set(mvp);
            });
            
            m_texturePipeline.Draw(s =>
            {
                s.Mvp.Set(mvp);
            });
            
            m_framebufferTexturePipeline.Draw(s =>
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                s.Mvp.Set(mvp);
                s.FramebufferTexture.Set(TextureUnit.Texture0);
            });

            m_renderDimensions.Clear();
            m_depthIndex = 0;
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
            
            m_pointsPipeline.Dispose();
            m_linesPipeline.Dispose();
            m_shapesPipeline.Dispose();
            m_texturePipeline.Dispose();
            m_framebufferTexturePipeline.Dispose();

            m_disposed = true;
        }
    }
}
