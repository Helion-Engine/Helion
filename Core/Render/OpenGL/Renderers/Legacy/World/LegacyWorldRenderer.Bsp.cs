using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Shared;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World.ViewClipping;
using Helion.World;
using Helion.World.Bsp;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public partial class LegacyWorldRenderer
{
    private readonly ViewClipper m_viewClipper = new();
    private FrustumPlanes m_frustumPlanes;

    private void TraverseBsp(IWorld world, RenderInfo renderInfo)
    {
        Frustum.SetFrustumPlanes(ref renderInfo.Uniforms.MvpNoPitch, ref m_frustumPlanes);

        var position = renderInfo.Camera.PositionInterpolated.XY.Double;
        var position3D = renderInfo.Camera.PositionInterpolated.Double;
        var viewDirection = renderInfo.Camera.Direction.XY.Double;

        m_geometryRenderer.ClearBsp();
        m_geometryRenderer.SetRenderMode(GeometryRenderMode.All, renderInfo.TransferHeightView);
        m_geometryRenderer.SetViewPosition(m_renderData.ViewPos3D, m_renderData.ViewPosInterpolated3D);

        m_viewClipper.Clear();
        m_viewClipper.Center = position;

        RecursivelyRenderBsp((uint)world.BspTree.Nodes.Length - 1, position3D, position3D.XY, viewDirection, world);
        m_lastTicker = world.GameTicker;
    }

    private unsafe void RecursivelyRenderBsp(uint nodeIndex, in Vec3D position, in Vec2D pos2D, in Vec2D viewDirection, IWorld world)
    {
        while ((nodeIndex & BspNodeCompact.IsSubsectorBit) == 0)
        {
            ref var node = ref world.BspTree.Nodes[nodeIndex];
            if (Occluded(node.BoundingBox, pos2D, viewDirection))
                return;

            var onRight = (node.SplitDelta.X * (position.Y - node.SplitStart.Y)) - (node.SplitDelta.Y * (position.X - node.SplitStart.X)) < 0;
            int front = *(byte*)&onRight;
            int back = front ^ 1;

            RecursivelyRenderBsp(node.Children[front], position, pos2D, viewDirection, world);
            nodeIndex = node.Children[back];
        }

        var subsector = world.BspTree.Subsectors[nodeIndex & BspNodeCompact.SubsectorMask];
        if (Occluded(subsector.BoundingBox, pos2D, viewDirection))
            return;

        var hasRenderedSector = subsector.Sector.CheckCount == m_renderData.CheckCount;
        m_geometryRenderer.RenderSubsector(subsector, position, pos2D, hasRenderedSector);

        // Entities are rendered by the sector
        if (hasRenderedSector)
            return;

        subsector.Sector.CheckCount = m_renderData.CheckCount;

        for (var node = subsector.Sector.Entities.Head; node != null; node = node.Next)
            m_entityRenderer.RenderEntity(node.Value, pos2D, subsector.Id);
    }

    private bool Occluded(in Box2D box, in Vec2D position, in Vec2D viewDirection)
    {
        // TODO is this needed?
        if (box.Contains(position))
            return false;

        if (m_occlude && !m_frustumPlanes.BoxInFront(box))
            return true;

        box.GetSpanningEdge(position, out var first, out var second);
        return m_viewClipper.InsideAnyRange(first, second);
    }
}
