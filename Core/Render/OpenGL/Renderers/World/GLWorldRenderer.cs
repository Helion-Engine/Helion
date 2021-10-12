using System;
using System.Drawing;
using Helion.Geometry.Boxes;
using Helion.Geometry.Planes;
using Helion.Geometry.Quads;
using Helion.Geometry.Rays;
using Helion.Geometry.Segments;
using Helion.Geometry.Spheres;
using Helion.Geometry.Triangles;
using Helion.Render.Common.Context;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL.Renderers.World.Geometry;
using Helion.Render.OpenGL.Renderers.World.Images;
using Helion.Render.OpenGL.Renderers.World.Primitives;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Textures.Buffer;
using Helion.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World;

/// <summary>
/// A renderer that renders the world and its components. It primarily
/// coordinates a lot of modules to perform the rendering.
/// </summary>
public class GLWorldRenderer : IWorldRenderContext
{
    private readonly PrimitiveWorldRenderer m_primitiveRenderer = new();
    private readonly ImageWorldRenderer m_imageRenderer = new();
    private readonly GeometryRenderer m_geometryRenderer;
    private bool m_disposed;

    public GLWorldRenderer(GLTextureManager textureManager, GLTextureDataBuffer textureDataBuffer)
    {
        m_geometryRenderer = new GeometryRenderer(textureManager, textureDataBuffer);
    }

    ~GLWorldRenderer()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public void Draw(IWorld world) => m_geometryRenderer.Draw(world);
    public void DrawLine(Seg3D seg, Color color) => m_primitiveRenderer.DrawLine(seg, color);
    public void DrawLines(Seg3D[] segs, Color color) => m_primitiveRenderer.DrawLines(segs, color);
    public void DrawRay(Ray3D ray, Color color) => m_primitiveRenderer.DrawRay(ray, color);
    public void DrawRays(Ray3D[] rays, Color color) => m_primitiveRenderer.DrawRays(rays, color);
    public void DrawTriangle(Triangle3D triangle, Color color) => m_primitiveRenderer.DrawTriangle(triangle, color);
    public void DrawTriangles(Triangle3D[] triangles, Color color) => m_primitiveRenderer.DrawTriangles(triangles, color);
    public void FillTriangle(Triangle3D triangle, Color color) => m_primitiveRenderer.FillTriangle(triangle, color);
    public void FillTriangles(Triangle3D[] triangles, Color color) => m_primitiveRenderer.FillTriangles(triangles, color);
    public void DrawQuad(Quad3D quad, Color color) => m_primitiveRenderer.DrawQuad(quad, color);
    public void DrawQuads(Quad3D[] quads, Color color) => m_primitiveRenderer.DrawQuads(quads, color);
    public void FillQuad(Quad3D quad, Color color) => m_primitiveRenderer.FillQuad(quad, color);
    public void FillQuads(Quad3D[] quads, Color color) => m_primitiveRenderer.FillQuads(quads, color);
    public void FillPlane(PlaneD plane, Color color) => m_primitiveRenderer.FillPlane(plane, color);
    public void FillPlanes(PlaneD[] planes, Color color) => m_primitiveRenderer.FillPlanes(planes, color);
    public void DrawBox(Box3D box, Color color) => m_primitiveRenderer.DrawBox(box, color);
    public void DrawBoxes(Box3D[] boxes, Color color) => m_primitiveRenderer.DrawBoxes(boxes, color);
    public void FillBox(Box3D box, Color color) => m_primitiveRenderer.FillBox(box, color);
    public void FillBoxes(Box3D[] boxes, Color color) => m_primitiveRenderer.FillBoxes(boxes, color);
    public void DrawSphere(Sphere3D sphere, Color color) => m_primitiveRenderer.DrawSphere(sphere, color);
    public void DrawSpheres(Sphere3D[] spheres, Color color) => m_primitiveRenderer.DrawSpheres(spheres, color);
    public void FillSphere(Sphere3D sphere, Color color) => m_primitiveRenderer.FillSphere(sphere, color);
    public void FillSpheres(Sphere3D[] spheres, Color color) => m_primitiveRenderer.FillSpheres(spheres, color);
    public void DrawImage(string texture, Quad3D quad, Color? color = null) => m_imageRenderer.DrawImage(texture, quad, color);
    public void DrawSurface(string surfaceName, Quad3D quad, Color? color = null) => m_imageRenderer.DrawSurface(surfaceName, quad, color);

    internal void Render(WorldRenderContext context)
    {
        m_geometryRenderer.Render(context);
        m_primitiveRenderer.Render(context);
        m_imageRenderer.Render(context);
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

        m_geometryRenderer.Dispose();
        m_primitiveRenderer.Dispose();
        m_imageRenderer.Dispose();

        m_disposed = true;
    }
}
