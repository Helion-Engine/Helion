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
using Helion.Render.OpenGL.Pipeline;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Primitives;

public class PrimitiveWorldRenderer : IDisposable
{
    private readonly RenderPipeline<PrimitiveWorldShader, PrimitiveWorldVertex> m_linePipeline;
    private readonly RenderPipeline<PrimitiveWorldShader, PrimitiveWorldVertex> m_trianglePipeline;
    private bool m_disposed;

    public PrimitiveWorldRenderer()
    {
        m_linePipeline = new RenderPipeline<PrimitiveWorldShader, PrimitiveWorldVertex>(
            "World line primitives", BufferUsageHint.StreamDraw, PrimitiveType.Lines);
        m_trianglePipeline = new RenderPipeline<PrimitiveWorldShader, PrimitiveWorldVertex>(
            "World triangle primitives", BufferUsageHint.StreamDraw, PrimitiveType.Triangles);
    }

    ~PrimitiveWorldRenderer()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public void DrawLine(Seg3D seg, Color color)
    {
        // TODO
    }

    public void DrawLines(Seg3D[] segs, Color color)
    {
        // TODO
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
        // TODO
    }

    public void DrawTriangles(Triangle3D[] triangles, Color color)
    {
        // TODO
    }

    public void FillTriangle(Triangle3D triangle, Color color)
    {
        // TODO
    }

    public void FillTriangles(Triangle3D[] triangles, Color color)
    {
        // TODO
    }

    public void DrawQuad(Quad3D quad, Color color)
    {
        // TODO
    }

    public void DrawQuads(Quad3D[] quads, Color color)
    {
        // TODO
    }

    public void FillQuad(Quad3D quad, Color color)
    {
        // TODO
    }

    public void FillQuads(Quad3D[] quads, Color color)
    {
        // TODO
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

    public void Render(WorldRenderContext context)
    {
        // TODO
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

        m_linePipeline.Dispose();
        m_trianglePipeline.Dispose();

        m_disposed = true;
    }
}

