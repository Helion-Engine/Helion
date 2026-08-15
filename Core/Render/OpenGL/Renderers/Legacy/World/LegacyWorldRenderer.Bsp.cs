using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Shared;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World.ViewClipping;
using Helion.World;
using Helion.World.Bsp;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public partial class LegacyWorldRenderer
{
    private readonly ViewClipper m_viewClipperPrev = new();
    private readonly ViewClipper m_viewClipper = new();
    private FrustumPlanes m_frustumPlanes;

    private void TraverseBsp(IWorld world, RenderInfo renderInfo)
    {
        Frustum.SetFrustumPlanes(ref renderInfo.Uniforms.MvpNoPitch, ref m_frustumPlanes);

        m_geometryRenderer.ClearBsp();
        m_geometryRenderer.SetViewPosition(m_renderData.ViewPos3D, m_renderData.ViewPosInterpolated3D);

        m_viewClipperPrev.Clear();
        m_viewClipper.Clear();
        m_viewClipperPrev.Center = m_renderData.ViewPosInterpolated;
        m_viewClipper.Center = m_renderData.ViewPos3D.XY;

        RecursivelyRenderBsp((uint)world.BspTree.Nodes.Length - 1, m_renderData.ViewPos3D, m_renderData.ViewPos3D.XY, m_renderData.ViewPosInterpolated, world);
        m_lastTicker = world.GameTicker;
    }

    private unsafe void RecursivelyRenderBsp(uint nodeIndex, in Vec3D position, in Vec2D pos2D, in Vec2D prevPos2D, IWorld world)
    {
        while ((nodeIndex & BspNodeCompact.IsSubsectorBit) == 0)
        {
            ref var node = ref world.BspTree.Nodes[nodeIndex];
            if (!ShouldRenderBox(node.BoundingBox, pos2D, prevPos2D))
                return;

            var onRight = (node.SplitDelta.X * (position.Y - node.SplitStart.Y)) - (node.SplitDelta.Y * (position.X - node.SplitStart.X)) < 0;
            int front = *(byte*)&onRight;
            int back = front ^ 1;

            RecursivelyRenderBsp(node.Children[front], position, pos2D, prevPos2D, world);
            nodeIndex = node.Children[back];
        }

        var subsector = world.BspTree.Subsectors[nodeIndex & BspNodeCompact.SubsectorMask];
        if (!ShouldRenderBox(subsector.BoundingBox, pos2D, prevPos2D))
            return;

        // Flats are rendered by sector, walls are rendered by subsector
        var hasRenderedSector = subsector.Sector.CheckCount == m_renderData.CheckCount;
        m_geometryRenderer.RenderSubsector(subsector, pos2D, prevPos2D, !hasRenderedSector);

        // Entities are rendered by the sector
        if (hasRenderedSector)
            return;

        subsector.Sector.CheckCount = m_renderData.CheckCount;

        var renderIndex = 0;
        for (var node = subsector.Sector.Entities.Head; node != null; node = node.Next)
        {
            if (node.Value.BlockmapCount == m_renderData.CheckCount)
                continue;
            node.Value.BlockmapCount = m_renderData.CheckCount;
            renderIndex = renderIndex % EntityRenderIndexMax;
            RenderEntity(world, node.Value, renderIndex++);
        }
    }

    private bool ShouldRenderBox(in Box2D box, in Vec2D pos2D, in Vec2D prevPos2D)
    {
        if (box.Contains(pos2D))
            return true;

        if (m_occlude && !m_frustumPlanes.BoxInFront(box))
            return false;

        // If not occluded in the first view clipper then don't check the second
        box.GetSpanningEdge(pos2D, out var first, out var second);
        if (!m_viewClipper.InsideAnyRange(first, second))
            return true;

        box.GetSpanningEdge(prevPos2D, out first, out second);
        return !m_viewClipperPrev.InsideAnyRange(first, second);
    }
}
