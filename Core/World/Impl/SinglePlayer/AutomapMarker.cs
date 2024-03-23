using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Shared.World.ViewClipping;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Subsectors;
using Helion.World.Physics;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Helion.World.Impl.SinglePlayer;

public class AutomapMarker
{
    private readonly struct PlayerPosition
    {
        public readonly Vec3D Position;
        public readonly Vec3D ViewDirection;
        public readonly double AngleRadians;
        public readonly double PitchRadians;

        public PlayerPosition(Vec3D position, Vec3D viewDirection, double angleRadians, double pitchRadians)
        {
            Position = position;
            ViewDirection = viewDirection;
            AngleRadians = angleRadians;
            PitchRadians = pitchRadians;
        }
    }

    private readonly LineDrawnTracker m_lineDrawnTracker = new();
    private readonly Stopwatch m_stopwatch = new();
    private readonly ViewClipper m_viewClipper;
    private Task? m_task;
    private CancellationTokenSource m_cancelTasks = new();
    private IWorld? m_world;
    private bool m_occlude;
    private Vec2D m_occludeViewPos;

    private readonly ConcurrentQueue<PlayerPosition> m_positions = new();

    public AutomapMarker(ArchiveCollection archiveCollection)
    {
        m_viewClipper = new ViewClipper(archiveCollection.DataCache);
    }

    public void Start(IWorld world)
    {
        if (m_task != null)
            return;

        ClearData();

        world.OnDestroying += World_OnDestroying;
        m_world = world;
        m_lineDrawnTracker.UpdateToWorld(world);
        m_task = Task.Factory.StartNew(() => AutomapTask(m_cancelTasks.Token), m_cancelTasks.Token,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private void World_OnDestroying(object? sender, EventArgs e)
    {
        if (m_world == null)
            return;

        m_world.OnDestroying -= World_OnDestroying;
        Stop();
        m_world = null;
    }

    public void Stop()
    {
        if (m_task == null)
            return;

        m_cancelTasks.Cancel();
        m_cancelTasks.Dispose();
        m_task.Wait();

        ClearData();

        m_cancelTasks = new CancellationTokenSource();
        m_task = null;
    }

    private void ClearData()
    {
        m_lineDrawnTracker.ClearDrawnLines();
        m_positions.Clear();
        m_viewClipper.Clear();
    }

    public void AddPosition(Vec3D pos, Vec3D viewDirection, double angleRadians, double pitchRadians)
    {
        m_positions.Enqueue(new PlayerPosition(pos, viewDirection, angleRadians, pitchRadians));
    }

    private void AutomapTask(CancellationToken token)
    {
        int ticks = (int)(1000 / Constants.TicksPerSecond);
        while (true)
        {
            if (token.IsCancellationRequested)
                return;

            m_stopwatch.Restart();

            while (m_world != null && m_positions.TryDequeue(out PlayerPosition pos))
            {
                if (token.IsCancellationRequested)
                    return;

                m_lineDrawnTracker.ClearDrawnLines();
                m_viewClipper.Clear();
                m_viewClipper.Center = pos.Position.XY;

                LegacyWorldRenderer.SetOccludePosition(pos.Position, pos.AngleRadians, pos.PitchRadians,
                    ref m_occlude, ref m_occludeViewPos);
                MarkBspLineClips((uint)m_world.BspTree.Nodes.Length - 1, pos.Position, pos.ViewDirection.XY, m_world, token);
            }

            m_stopwatch.Stop();
            if (m_stopwatch.ElapsedMilliseconds >= ticks)
                continue;

            Thread.Sleep(Math.Max(ticks - (int)m_stopwatch.ElapsedMilliseconds, 0));
        }
    }

    private unsafe void MarkBspLineClips(uint nodeIndex, in Vec3D position, in Vec2D viewDirection, IWorld world, CancellationToken token)
    {
        Vec2D pos2D = position.XY;
        while ((nodeIndex & BspNodeCompact.IsSubsectorBit) == 0)
        {
            fixed (BspNodeCompact* node = &world.BspTree.Nodes[nodeIndex])
            {
                if (Occluded(node->BoundingBox, pos2D, viewDirection))
                    return;

                int front = Convert.ToInt32(node->Splitter.PerpDot(pos2D) < 0);
                int back = front ^ 1;

                MarkBspLineClips(node->Children[front], position, viewDirection, world, token);
                nodeIndex = node->Children[back];
            }

            if (token.IsCancellationRequested)
                return;
        }

        Subsector subsector = world.BspTree.Subsectors[nodeIndex & BspNodeCompact.SubsectorMask];
        if (Occluded(subsector.BoundingBox, pos2D, viewDirection))
            return;

        for (int i = 0; i < subsector.ClockwiseEdges.Count; i++)
        {
            var edge = subsector.ClockwiseEdges[i];
            if (edge.SideId == null)
                continue;

            var side = m_world.Sides[edge.SideId.Value];
            Line line = side.Line;
            if (m_lineDrawnTracker.HasDrawn(line))
            {
                AddLineClip(edge);
                continue;
            }

            if (line.OneSided && !line.Segment.OnRight(pos2D))
                continue;

            if (m_viewClipper.InsideAnyRange(line.Segment.Start, line.Segment.End))
                continue;

            AddLineClip(edge);
            m_lineDrawnTracker.MarkDrawn(line);

            if (line.SeenForAutomap)
                continue;

            if (m_occlude && !line.Segment.InView(pos2D, viewDirection))
                continue;

            line.MarkSeenOnAutomap();
        }
    }

    private void AddLineClip(SubsectorSegment edge)
    {
        var side = m_world.Sides[edge.SideId!.Value];
        if (side.Line.OneSided)
            m_viewClipper.AddLine(edge.Start, edge.End);
        else if (LineOpening.IsRenderingBlocked(side.Line))
            m_viewClipper.AddLine(edge.Start, edge.End);
    }

    private bool Occluded(in Box2D box, in Vec2D position, in Vec2D viewDirection)
    {
        if (box.Contains(position))
            return false;

        if (m_occlude && !box.InView(m_occludeViewPos, viewDirection))
            return true;

        box.GetSpanningEdge(position, out var first, out var second);
        return m_viewClipper.InsideAnyRange(first, second);
    }
}
