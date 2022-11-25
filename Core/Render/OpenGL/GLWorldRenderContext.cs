using System.Drawing;
using Helion.Geometry.Boxes;
using Helion.Geometry.Planes;
using Helion.Geometry.Quads;
using Helion.Geometry.Rays;
using Helion.Geometry.Segments;
using Helion.Geometry.Spheres;
using Helion.Geometry.Triangles;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Context;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.World;
using Helion.Render.OpenGL.Commands;
using Helion.World;
using Helion.World.Impl.SinglePlayer;

namespace Helion.Render.OpenGL;

public class GLLegacyWorldRenderContext : IWorldRenderContext
{
    private readonly Shared.OldCamera m_oldCamera = new(Vec3F.Zero, 0, 0);
    private readonly RenderCommands m_commands;
    private WorldRenderContext? m_context;

    public GLLegacyWorldRenderContext(RenderCommands commands)
    {
        m_commands = commands;
    }

    internal void Begin(WorldRenderContext context)
    {
        m_context = context;
    }

    public void Draw(IWorld world)
    {
        Camera camera = m_context.Camera;
        Vec3F cameraPosition = camera.Position(m_context.InterpolationFrac);
        m_oldCamera.Set(cameraPosition, camera.YawRadians, camera.PitchRadians);

        // Note: We never draw the automap for this, that should be handled
        // elsewhere.
        m_commands.DrawWorld(world, m_oldCamera, world.Gametick, m_context.InterpolationFrac,
            world.Player, m_context?.DrawAutomap ?? false, m_context?.AutomapOffset ?? (0, 0),
            m_context?.AutomapScale ?? 1.0);
    }

    public void DrawLine(Seg3D seg, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawLines(Seg3D[] segs, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawRay(Ray3D ray, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawRays(Ray3D[] rays, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawTriangle(Triangle3D triangle, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawTriangles(Triangle3D[] triangles, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void FillTriangle(Triangle3D triangle, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void FillTriangles(Triangle3D[] triangles, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawQuad(Quad3D quad, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawQuads(Quad3D[] quads, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void FillQuad(Quad3D quad, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void FillQuads(Quad3D[] quads, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void FillPlane(PlaneD plane, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void FillPlanes(PlaneD[] planes, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawBox(Box3D box, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawBoxes(Box3D[] boxes, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void FillBox(Box3D box, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void FillBoxes(Box3D[] boxes, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawSphere(Sphere3D sphere, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawSpheres(Sphere3D[] spheres, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void FillSphere(Sphere3D sphere, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void FillSpheres(Sphere3D[] spheres, Color color)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawImage(string texture, Quad3D quad, Color? color = null)
    {
        // Not implemented in the legacy renderer.
    }

    public void DrawSurface(string surfaceName, Quad3D quad, Color? color = null)
    {
        // Not implemented in the legacy renderer.
    }

    public void Dispose()
    {
        // Nothing to do.
    }
}
