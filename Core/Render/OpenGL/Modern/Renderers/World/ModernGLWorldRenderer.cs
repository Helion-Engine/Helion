using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GlmSharp;
using Helion.Geometry.Boxes;
using Helion.Geometry.Planes;
using Helion.Geometry.Quads;
using Helion.Geometry.Rays;
using Helion.Geometry.Segments;
using Helion.Geometry.Spheres;
using Helion.Geometry.Triangles;
using Helion.Render.Common;
using Helion.Render.Common.Framebuffer;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.World;
using Helion.Render.OpenGL.Modern.Renderers.Hud.Primitives;
using Helion.Render.OpenGL.Modern.Textures;
using Helion.Render.OpenGL.Primitives;
using Helion.World;
using OpenTK.Graphics.OpenGL4;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Modern.Renderers.World
{
    // TODO: Should listen to world.OnDispose events when the time comes!
    public class ModernGLWorldRenderer : IWorldRenderer
    {
        private readonly ModernGLTextureManager m_textureManager;
        private readonly PrimitiveRenderPipeline m_lines = new("World primitive lines", PrimitiveType.Lines);
        private readonly PrimitiveRenderPipeline m_triangles = new("World primitive triangles", PrimitiveType.Triangles);
        private readonly Dictionary<WeakReference<IWorld>, ModernGLLevelRenderer> m_levelsToRender = new();
        private bool m_disposed;

        public ModernGLWorldRenderer(ModernGLTextureManager textureManager)
        {
            m_textureManager = textureManager;
        }
        
        ~ModernGLWorldRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        private bool AlreadyTrackingWorld(IWorld world)
        {
            foreach (WeakReference<IWorld> worldRef in m_levelsToRender.Keys)
                if (worldRef.TryGetTarget(out IWorld? target) && ReferenceEquals(target, world))
                    return true;
            return false;
        }

        public void Draw(IWorld world)
        {
            if (AlreadyTrackingWorld(world))
                return;

            WeakReference<IWorld> worldReference = new(world);
            ModernGLLevelRenderer levelRenderer = new(world, m_textureManager);
            m_levelsToRender[worldReference] = levelRenderer;
        }

        private void AddLine(Seg3D seg, ByteColor color)
        {
            HudPrimitiveVertex start = new(seg.Start.Float, color);
            m_lines.Vbo.Add(start);
            
            HudPrimitiveVertex end = new(seg.End.Float, color);
            m_lines.Vbo.Add(end);
        }

        public void DrawLine(Seg3D seg, Color color)
        {
            ByteColor byteColor = new ByteColor(color);
            AddLine(seg, byteColor);
        }

        public void DrawLines(Seg3D[] segs, Color color)
        {
            ByteColor byteColor = new ByteColor(color);
            
            for (int i = 0; i < segs.Length; i++)
                AddLine(segs[i], byteColor);
        }

        public void DrawRay(Ray3D ray, Color color)
        {
            // TODO
        }

        public void DrawRays(Ray3D[] rays, Color color)
        {
            // TODO
        }

        public void DrawTriangle(Triangle3D triangle, Color color)
        {
            Seg3D firstSeg = (triangle.First, triangle.Second);
            Seg3D secondSeg = (triangle.Second, triangle.Third);
            Seg3D thirdSeg = (triangle.Third, triangle.First);

            ByteColor byteColor = new(color);
            AddLine(firstSeg, byteColor);
            AddLine(secondSeg, byteColor);
            AddLine(thirdSeg, byteColor);
        }

        public void DrawTriangles(Triangle3D[] triangles, Color color)
        {
            ByteColor byteColor = new(color);

            for (int i = 0; i < triangles.Length; i++)
            {
                Triangle3D triangle = triangles[i];
                
                Seg3D firstSeg = (triangle.First, triangle.Second);
                Seg3D secondSeg = (triangle.Second, triangle.Third);
                Seg3D thirdSeg = (triangle.Third, triangle.First);

                AddLine(firstSeg, byteColor);
                AddLine(secondSeg, byteColor);
                AddLine(thirdSeg, byteColor);   
            }
        }

        private void AddTriangle(Triangle3D triangle, ByteColor color)
        {
            HudPrimitiveVertex first = new(triangle.First.Float, color);
            HudPrimitiveVertex second = new(triangle.Second.Float, color);
            HudPrimitiveVertex third = new(triangle.Third.Float, color);
            
            m_triangles.Vbo.Add(first);
            m_triangles.Vbo.Add(second);
            m_triangles.Vbo.Add(third);
        }

        public void FillTriangle(Triangle3D triangle, Color color)
        {
            ByteColor byteColor = new(color);
            AddTriangle(triangle, byteColor);
        }

        public void FillTriangles(Triangle3D[] triangles, Color color)
        {
            ByteColor byteColor = new ByteColor(color);
            
            for (int i = 0; i < triangles.Length; i++)
                AddTriangle(triangles[i], byteColor);
        }

        public void DrawQuad(Quad3D quad, Color color)
        {
            ByteColor byteColor = new ByteColor(color);

            Seg3D top = new(quad.TopLeft, quad.TopRight);
            Seg3D bottom = new(quad.BottomLeft, quad.BottomRight);
            Seg3D left = new(quad.TopLeft, quad.BottomLeft);
            Seg3D right = new(quad.TopRight, quad.BottomRight);
            
            AddLine(top, byteColor);
            AddLine(bottom, byteColor);
            AddLine(left, byteColor);
            AddLine(right, byteColor);
        }

        public void DrawQuads(Quad3D[] quads, Color color)
        {
            ByteColor byteColor = new ByteColor(color);

            for (int i = 0; i < quads.Length; i++)
            {
                Quad3D quad = quads[i];

                Seg3D top = new(quad.TopLeft, quad.TopRight);
                Seg3D bottom = new(quad.BottomLeft, quad.BottomRight);
                Seg3D left = new(quad.TopLeft, quad.BottomLeft);
                Seg3D right = new(quad.TopRight, quad.BottomRight);
            
                AddLine(top, byteColor);
                AddLine(bottom, byteColor);
                AddLine(left, byteColor);
                AddLine(right, byteColor);
            }
        }

        public void FillQuad(Quad3D quad, Color color)
        {
            ByteColor byteColor = new ByteColor(color);
            Triangle3D top = new(quad.TopLeft, quad.BottomLeft, quad.TopRight);
            Triangle3D bottom = new(quad.TopRight, quad.BottomLeft, quad.BottomRight);
            
            AddTriangle(top, byteColor);
            AddTriangle(bottom, byteColor);
        }

        public void FillQuads(Quad3D[] quads, Color color)
        {
            ByteColor byteColor = new ByteColor(color);
            
            for (int i = 0; i < quads.Length; i++)
            {
                Quad3D quad = quads[i];
                
                Triangle3D top = new(quad.TopLeft, quad.BottomLeft, quad.TopRight);
                Triangle3D bottom = new(quad.TopRight, quad.BottomLeft, quad.BottomRight);
            
                AddTriangle(top, byteColor);
                AddTriangle(bottom, byteColor);   
            }
        }

        public void FillPlane(PlaneD plane, Color color)
        {
            // TODO
        }

        public void FillPlanes(PlaneD[] planes, Color color)
        {
            // TODO
        }

        public void DrawBox(Box3D box, Color color)
        {
            // TODO
        }

        public void DrawBoxes(Box3D[] boxes, Color color)
        {
            // TODO
        }

        public void FillBox(Box3D box, Color color)
        {
            // TODO
        }

        public void FillBoxes(Box3D[] boxes, Color color)
        {
            // TODO
        }

        public void DrawSphere(Sphere3D sphere, Color color)
        {
            // TODO
        }

        public void DrawSpheres(Sphere3D[] spheres, Color color)
        {
            // TODO
        }

        public void FillSphere(Sphere3D sphere, Color color)
        {
            // TODO
        }

        public void FillSpheres(Sphere3D[] spheres, Color color)
        {
            // TODO
        }

        public void DrawImage(IRenderableTexture texture, Quad3D quad, Color? color)
        {
            // TODO
        }

        public void DrawFrameBuffer(IFramebuffer framebuffer, Quad3D quad, Color? color)
        {
            // TODO
        }

        private static mat4 CreateMvpMatrix(WorldRenderContext context)
        {
            const float zNear = 1.0f; // TODO
            const float zFar = 65536.0f; // TODO
            float fovY = glm.Radians(63.2f);

            mat4 model = mat4.Identity;
            mat4 view = context.Camera.ViewMatrix(context.InterpolationFrac);
            mat4 proj = mat4.Perspective(fovY, context.Viewport.AspectRatio, zNear, zFar);

            return proj * view * model;
        }

        private void RemoveDisposedWorlds()
        {
            var levelsToRemove = m_levelsToRender.Keys.Where(r => !r.TryGetTarget(out _)).ToList();
            foreach (WeakReference<IWorld> key in levelsToRemove)
                m_levelsToRender.Remove(key);
        }

        public void Render(WorldRenderContext context)
        {
            if (m_disposed)
                return;

            mat4 mvp = CreateMvpMatrix(context);

            RemoveDisposedWorlds();
            foreach (ModernGLLevelRenderer levelRenderer in m_levelsToRender.Values)
                levelRenderer.Draw(context.Camera, mvp);

            m_lines.Draw(s =>
            {
                s.Mvp.Set(mvp);
            });
            
            m_triangles.Draw(s =>
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
            
            m_lines.Dispose();
            m_triangles.Dispose();
            foreach (ModernGLLevelRenderer levelRenderer in m_levelsToRender.Values)
                levelRenderer.Dispose();

            m_disposed = true;
        }
    }
}
