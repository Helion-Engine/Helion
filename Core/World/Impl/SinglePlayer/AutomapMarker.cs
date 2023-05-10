using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Renderers.Legacy.World.Entities;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Shared.World.ViewClipping;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Subsectors;
using Helion.World.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Helion.World.Impl.SinglePlayer;

public class AutomapMarker
{
    private readonly LegacyWorldRenderer m_worldRenderer;
    private Task? m_task;
    private CancellationTokenSource _cancelTasks = new();
    private ViewClipper m_viewClipper;
    private readonly LineDrawnTracker m_lineDrawnTracker = new();

    public AutomapMarker(ArchiveCollection archiveCollection, LegacyWorldRenderer worldRenderer)
    {
        m_worldRenderer = worldRenderer;
        m_viewClipper = new ViewClipper(archiveCollection.DataCache);
    }

    public void Start()
    {
        return;
        if (m_task != null)
            return;

        _cancelTasks = new CancellationTokenSource();
        m_task = Task.Factory.StartNew(() => AutomapTask(_cancelTasks.Token), _cancelTasks.Token,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public async void Stop()
    {
        if (m_task == null)
            return;

        _cancelTasks.Cancel();
        _cancelTasks.Dispose();
        m_task.Wait();
    }

    private void AutomapTask(CancellationToken token)
    {
        while (true)
        {
            if (token.IsCancellationRequested)
                return;

            IWorld world = m_worldRenderer.World;
            if (world != null)
            {
                m_lineDrawnTracker.ClearDrawnLines();
                var camera = world.Player.GetCamera(0);
                m_viewClipper.Clear();
                m_viewClipper.Center = camera.Position.Double.XY;
                MarkBspLineClips((uint)world.BspTree.Nodes.Length - 1, camera.Position.Double, camera.Direction.Double.XY, world);
            }

            Task.Delay(((int)Constants.TicksPerSecond), token);
        }
    }

    private unsafe void MarkBspLineClips(uint nodeIndex, in Vec3D position, in Vec2D viewDirection, IWorld world)
    {
        Vec2D pos2D = position.XY;
        while ((nodeIndex & BspNodeCompact.IsSubsectorBit) == 0)
        {
            fixed (BspNodeCompact* node = &world.BspTree.Nodes[nodeIndex])
            {
                if (Occluded(node->BoundingBox, pos2D))
                    return;

                int front = Convert.ToInt32(node->Splitter.PerpDot(pos2D) < 0);
                int back = front ^ 1;

                MarkBspLineClips(node->Children[front], position, viewDirection, world);
                nodeIndex = node->Children[back];
            }
        }

        Subsector subsector = world.BspTree.Subsectors[nodeIndex & BspNodeCompact.SubsectorMask];
        if (Occluded(subsector.BoundingBox, pos2D))
            return;

        for (int i = 0; i < subsector.ClockwiseEdges.Count; i++)
        {
            var edge = subsector.ClockwiseEdges[i];
            if (edge.Side == null)
                continue;

            Line line = edge.Side.Line;
            line.MarkSeenOnAutomap();

            if (m_lineDrawnTracker.HasDrawn(line))
            {
                AddLineClip(edge);
                continue;
            }

            m_lineDrawnTracker.MarkDrawn(line);

            bool onFrontSide = line.Segment.OnRight(pos2D);
            if (!onFrontSide && line.OneSided)
                continue;

            AddLineClip(edge);
        }
    }

    private unsafe void AddLineClip(SubsectorSegment edge)
    {
        if (edge.Side!.Line.OneSided)
            m_viewClipper.AddLine(edge.Start, edge.End);
        else if (LineOpening.IsRenderingBlocked(edge.Side.Line))
            m_viewClipper.AddLine(edge.Start, edge.End);
    }

    private bool Occluded(in Box2D box, in Vec2D position)
    {
        if (box.Contains(position))
            return false;

        (Vec2D first, Vec2D second) = box.GetSpanningEdge(position);
        return m_viewClipper.InsideAnyRange(first, second);
    }
}
