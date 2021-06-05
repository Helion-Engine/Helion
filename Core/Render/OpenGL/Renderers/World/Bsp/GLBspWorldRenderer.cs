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
using Helion.Render.Common.World;
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
        private bool m_disposed;
        
        ~GLBspWorldRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public override void Draw(IWorld world)
        {
            throw new NotImplementedException();
        }

        public override void DrawLine(Seg3D seg, Color color)
        {
            throw new NotImplementedException();
        }

        public override void DrawLines(Seg3D[] segs, Color color)
        {
            throw new NotImplementedException();
        }

        public override void DrawRay(Ray3D ray, Color color)
        {
            throw new NotImplementedException();
        }

        public override void DrawRays(Ray3D[] rays, Color color)
        {
            throw new NotImplementedException();
        }

        public override void DrawTriangle(Triangle3D triangle, Color color)
        {
            throw new NotImplementedException();
        }

        public override void DrawTriangles(Triangle3D[] triangles, Color color)
        {
            throw new NotImplementedException();
        }

        public override void FillTriangle(Triangle3D triangle, Color color)
        {
            throw new NotImplementedException();
        }

        public override void FillTriangles(Triangle3D[] triangles, Color color)
        {
            throw new NotImplementedException();
        }

        public override void DrawQuad(Quad3D quad, Color color)
        {
            throw new NotImplementedException();
        }

        public override void DrawQuads(Quad3D[] quads, Color color)
        {
            throw new NotImplementedException();
        }

        public override void FillQuad(Quad3D quad, Color color)
        {
            throw new NotImplementedException();
        }

        public override void FillQuads(Quad3D[] quads, Color color)
        {
            throw new NotImplementedException();
        }

        public override void FillPlane(PlaneD plane, Color color)
        {
            throw new NotImplementedException();
        }

        public override void FillPlanes(PlaneD[] planes, Color color)
        {
            throw new NotImplementedException();
        }

        public override void DrawBox(Box3D box, Color color)
        {
            throw new NotImplementedException();
        }

        public override void DrawBoxes(Box3D[] boxes, Color color)
        {
            throw new NotImplementedException();
        }

        public override void FillBox(Box3D box, Color color)
        {
            throw new NotImplementedException();
        }

        public override void FillBoxes(Box3D[] boxes, Color color)
        {
            throw new NotImplementedException();
        }

        public override void DrawSphere(Sphere3D sphere, Color color)
        {
            throw new NotImplementedException();
        }

        public override void DrawSpheres(Sphere3D[] spheres, Color color)
        {
            throw new NotImplementedException();
        }

        public override void FillSphere(Sphere3D sphere, Color color)
        {
            throw new NotImplementedException();
        }

        public override void FillSpheres(Sphere3D[] spheres, Color color)
        {
            throw new NotImplementedException();
        }

        public override void DrawImage(IRenderableTexture texture, Quad3D quad, Color? color)
        {
            throw new NotImplementedException();
        }

        public override void Render(WorldRenderContext context)
        {
            throw new NotImplementedException();
        }

        internal override void Begin()
        {
            throw new NotImplementedException();
        }

        internal override void End()
        {
            throw new NotImplementedException();
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
            
            // TODO

            m_disposed = true;
        }
    }
}
