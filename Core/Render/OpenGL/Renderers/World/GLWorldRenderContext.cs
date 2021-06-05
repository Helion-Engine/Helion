using System.Drawing;
using Helion.Geometry.Boxes;
using Helion.Geometry.Planes;
using Helion.Geometry.Quads;
using Helion.Geometry.Rays;
using Helion.Geometry.Segments;
using Helion.Geometry.Spheres;
using Helion.Geometry.Triangles;
using Helion.Render.Common;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.World;
using Helion.World;

namespace Helion.Render.OpenGL.Renderers.World
{
    public abstract class GLWorldRenderContext : IWorldRenderContext
    {
        internal abstract void Begin();
        internal abstract void End();
        public abstract void Draw(IWorld world);
        public abstract void DrawLine(Seg3D seg, Color color);
        public abstract void DrawLines(Seg3D[] segs, Color color);
        public abstract void DrawRay(Ray3D ray, Color color);
        public abstract void DrawRays(Ray3D[] rays, Color color);
        public abstract void DrawTriangle(Triangle3D triangle, Color color);
        public abstract void DrawTriangles(Triangle3D[] triangles, Color color);
        public abstract void FillTriangle(Triangle3D triangle, Color color);
        public abstract void FillTriangles(Triangle3D[] triangles, Color color);
        public abstract void DrawQuad(Quad3D quad, Color color);
        public abstract void DrawQuads(Quad3D[] quads, Color color);
        public abstract void FillQuad(Quad3D quad, Color color);
        public abstract void FillQuads(Quad3D[] quads, Color color);
        public abstract void FillPlane(PlaneD plane, Color color);
        public abstract void FillPlanes(PlaneD[] planes, Color color);
        public abstract void DrawBox(Box3D box, Color color);
        public abstract void DrawBoxes(Box3D[] boxes, Color color);
        public abstract void FillBox(Box3D box, Color color);
        public abstract void FillBoxes(Box3D[] boxes, Color color);
        public abstract void DrawSphere(Sphere3D sphere, Color color);
        public abstract void DrawSpheres(Sphere3D[] spheres, Color color);
        public abstract void FillSphere(Sphere3D sphere, Color color);
        public abstract void FillSpheres(Sphere3D[] spheres, Color color);
        public abstract void DrawImage(IRenderableTexture texture, Quad3D quad, Color? color);
        public abstract void Render(WorldRenderContext context);
        public abstract void Dispose();
    }
}
