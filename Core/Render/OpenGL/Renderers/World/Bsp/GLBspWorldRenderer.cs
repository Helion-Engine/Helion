using System;
using System.Drawing;
using Helion.Geometry.Boxes;
using Helion.Geometry.Planes;
using Helion.Geometry.Quads;
using Helion.Geometry.Rays;
using Helion.Geometry.Segments;
using Helion.Geometry.Spheres;
using Helion.Geometry.Triangles;
using Helion.Render.Common;
using Helion.Render.Common.Context;
using Helion.Render.OpenGL.Renderers.World.Primitives;
using Helion.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Bsp
{
    /// <summary>
    /// A renderer that walks the BSP tree for deciding what to render and in
    /// what order.
    /// </summary>
    /// <remarks>
    /// This is not related to the GLBSP tool.
    /// </remarks>
    public class GLBspWorldRenderer : GLWorldRenderer
    {
        private readonly GLPrimitiveWorldRenderer m_primitiveRenderer = new();
        private bool m_disposed;
        
        ~GLBspWorldRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public override void Draw(IWorld world)
        {
            // TODO
        }

        public override void DrawLine(Seg3D seg, Color color) => m_primitiveRenderer.DrawLine(seg, color);
        public override void DrawLines(Seg3D[] segs, Color color) => m_primitiveRenderer.DrawLines(segs, color);
        public override void DrawRay(Ray3D ray, Color color) => m_primitiveRenderer.DrawRay(ray, color);
        public override void DrawRays(Ray3D[] rays, Color color) => m_primitiveRenderer.DrawRays(rays, color);
        public override void DrawTriangle(Triangle3D triangle, Color color) => m_primitiveRenderer.DrawTriangle(triangle, color);
        public override void DrawTriangles(Triangle3D[] triangles, Color color) => m_primitiveRenderer.DrawTriangles(triangles, color);
        public override void FillTriangle(Triangle3D triangle, Color color) => m_primitiveRenderer.FillTriangle(triangle, color);
        public override void FillTriangles(Triangle3D[] triangles, Color color) => m_primitiveRenderer.FillTriangles(triangles, color);
        public override void DrawQuad(Quad3D quad, Color color) => m_primitiveRenderer.DrawQuad(quad, color);
        public override void DrawQuads(Quad3D[] quads, Color color) => m_primitiveRenderer.DrawQuads(quads, color);
        public override void FillQuad(Quad3D quad, Color color) => m_primitiveRenderer.FillQuad(quad, color);
        public override void FillQuads(Quad3D[] quads, Color color) => m_primitiveRenderer.FillQuads(quads, color);
        public override void FillPlane(PlaneD plane, Color color) => m_primitiveRenderer.FillPlane(plane, color);
        public override void FillPlanes(PlaneD[] planes, Color color) => m_primitiveRenderer.FillPlanes(planes, color);
        public override void DrawBox(Box3D box, Color color) => m_primitiveRenderer.DrawBox(box, color);
        public override void DrawBoxes(Box3D[] boxes, Color color) => m_primitiveRenderer.DrawBoxes(boxes, color);
        public override void FillBox(Box3D box, Color color) => m_primitiveRenderer.FillBox(box, color);
        public override void FillBoxes(Box3D[] boxes, Color color) => m_primitiveRenderer.FillBoxes(boxes, color);
        public override void DrawSphere(Sphere3D sphere, Color color) => m_primitiveRenderer.DrawSphere(sphere, color);
        public override void DrawSpheres(Sphere3D[] spheres, Color color) => m_primitiveRenderer.DrawSpheres(spheres, color);
        public override void FillSphere(Sphere3D sphere, Color color) => m_primitiveRenderer.FillSphere(sphere, color);
        public override void FillSpheres(Sphere3D[] spheres, Color color) => m_primitiveRenderer.FillSpheres(spheres, color);
        
        public override void DrawImage(IRenderableTexture texture, Quad3D quad, Color? color)
        {
            // TODO
        }

        internal override void Render(WorldRenderContext context)
        {
            // TODO
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;
            
            m_primitiveRenderer.Dispose();

            m_disposed = true;
        }
    }
}
