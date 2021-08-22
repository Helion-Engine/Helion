using System;
using System.Drawing;
using Helion.Geometry.Boxes;
using Helion.Geometry.Planes;
using Helion.Geometry.Quads;
using Helion.Geometry.Rays;
using Helion.Geometry.Segments;
using Helion.Geometry.Spheres;
using Helion.Geometry.Triangles;
using Helion.Render.Common.Textures;
using Helion.Resources;
using Helion.World;

namespace Helion.Render.Common.Renderers
{
    /// <summary>
    /// Performs world drawing commands. 
    /// </summary>
    public interface IWorldRenderContext : IDisposable
    {
        void Draw(IWorld world);
        void DrawLine(Seg3D seg, Color color);
        void DrawLines(Seg3D[] segs, Color color);
        void DrawRay(Ray3D ray, Color color);
        void DrawRays(Ray3D[] rays, Color color);
        void DrawTriangle(Triangle3D triangle, Color color);
        void DrawTriangles(Triangle3D[] triangles, Color color);
        void FillTriangle(Triangle3D triangle, Color color);
        void FillTriangles(Triangle3D[] triangles, Color color);
        void DrawQuad(Quad3D quad, Color color);
        void DrawQuads(Quad3D[] quads, Color color);
        void FillQuad(Quad3D quad, Color color);
        void FillQuads(Quad3D[] quads, Color color);
        void FillPlane(PlaneD plane, Color color);
        void FillPlanes(PlaneD[] planes, Color color);
        void DrawBox(Box3D box, Color color);
        void DrawBoxes(Box3D[] boxes, Color color);
        void FillBox(Box3D box, Color color);
        void FillBoxes(Box3D[] boxes, Color color);
        void DrawSphere(Sphere3D sphere, Color color);
        void DrawSpheres(Sphere3D[] spheres, Color color);
        void FillSphere(Sphere3D sphere, Color color);
        void FillSpheres(Sphere3D[] spheres, Color color);
        void DrawImage(string texture, Quad3D quad, Color? color = null);
        void DrawSurface(string surfaceName, Quad3D quad, Color? color = null);
    }
}
