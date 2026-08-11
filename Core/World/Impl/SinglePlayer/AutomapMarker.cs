using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Render;
using Helion.Render.Common.Shared;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World.ViewClipping;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Helion.World.Impl.SinglePlayer;

public class AutomapMarker
{
    private readonly struct PlayerPosition(Vec3D position, Vec3D viewDirection, double angleRadians, double pitchRadians)
    {
        public readonly Vec3D Position = position;
        public readonly Vec3D ViewDirection = viewDirection;
        public readonly double AngleRadians = angleRadians;
        public readonly double PitchRadians = pitchRadians;
    }

    private BitArray m_hitLines = new(0);
    private readonly Stopwatch m_stopwatch = new();
    private readonly ViewClipper m_viewClipper = new();
    private readonly RenderInfo m_renderInfo = new();
    private readonly OldCamera m_camera = new(default, default, 0, 0);
    private readonly Entity m_dummyEntity = new();
    private Task? m_task;
    private CancellationTokenSource m_cancelTasks = new();
    private IWorld m_world = null!;
    private FrustumPlanes m_frustumPlanes;

    private readonly ConcurrentQueue<PlayerPosition> m_positions = new();

    public void Start(IWorld world)
    {
        if (m_task != null)
            return;

        ClearData();

        world.OnDestroying += World_OnDestroying;
        m_world = world;
        m_hitLines = new(world.Lines.Count);

        m_dummyEntity.Set(0, 0, 0, EntityDefinition.Default, default, 0, m_world.Sectors[0], m_world, default);

        m_task = Task.Factory.StartNew(() => AutomapTask(m_cancelTasks.Token), m_cancelTasks.Token,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private void World_OnDestroying(object? sender, EventArgs e)
    {
        if (m_world == null)
            return;

        m_world.OnDestroying -= World_OnDestroying;
        Stop();
        m_world = null!;
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
        m_positions.Clear();
        m_viewClipper.Clear();
    }

    public void AddPosition(Vec3D pos, Vec3D viewDirection, double angleRadians, double pitchRadians)
    {
        m_positions.Enqueue(new PlayerPosition(pos, viewDirection, angleRadians, pitchRadians));
    }

    private void AutomapTask(CancellationToken token)
    {
        const int ClearCount = 5;
        int ticks = (int)(1000 / Constants.TicksPerSecond);
        while (true)
        {
            if (token.IsCancellationRequested)
                return;

            //var viewport = GetViewport();
            //m_stopwatch.Restart();

            //while (m_world != null && m_positions.TryDequeue(out PlayerPosition pos))
            //{
            //    // Don't let the queue fill up indefinitely when processing too slowly
            //    if (m_positions.Count > ClearCount)
            //        m_positions.Clear();

            //    if (token.IsCancellationRequested)
            //        return;

            //    m_viewClipper.Clear();
            //    m_viewClipper.Center = pos.Position.XY;
            //    m_hitLines.SetAll(false);

            //    SetFrustum(viewport, pos);
            //    MarkBspLineClips((uint)m_world.BspTree.Nodes.Length - 1, pos.Position.XY, m_world, token);
            //}

            //m_stopwatch.Stop();
            //if (m_stopwatch.ElapsedMilliseconds >= ticks)
            //    continue;

            Thread.Sleep(Math.Max(ticks - (int)m_stopwatch.ElapsedMilliseconds, 0));
        }
    }

    private void SetFrustum(Rectangle viewport, PlayerPosition pos)
    {
        var viewPosition = pos.Position.Float;
        m_camera.Set(viewPosition, viewPosition, (float)pos.AngleRadians, (float)pos.PitchRadians);
        m_renderInfo.Set(m_camera, 0, viewport, m_dummyEntity, false, default, 0, m_world.Config.Render, Sector.Default, default, default, default, default);
        var mvp = Renderer.CalculateMvpMatrix(m_renderInfo, onlyXY: true);
        Frustum.SetFrustumPlanes(ref mvp, ref m_frustumPlanes);
    }

    private Rectangle GetViewport()
    {
        var window = m_world.Config.Window;
        if (window.Virtual.Enable.Value)
            return new(0, 0, window.Virtual.Dimension.Value.Width, window.Virtual.Dimension.Value.Height);
        return new(0, 0, window.Dimension.Value.Width, window.Dimension.Value.Height);
    }

    private unsafe void MarkBspLineClips(uint nodeIndex, in Vec2D position, IWorld world, CancellationToken token)
    {
        while ((nodeIndex & BspNodeCompact.IsSubsectorBit) == 0)
        {
            fixed (BspNodeCompact* node = &world.BspTree.Nodes[nodeIndex])
            {
                bool onRight = (node->SplitDelta.X * (position.Y - node->SplitStart.Y)) - (node->SplitDelta.Y * (position.X - node->SplitStart.X)) < 0;
                int front = *(byte*)&onRight;

                MarkBspLineClips(node->Children[front], position, world, token);

                nodeIndex = node->Children[front ^ 1];
                if ((nodeIndex & BspNodeCompact.IsSubsectorBit) == 0)
                {
                    if (Occluded(world.BspTree.Nodes[nodeIndex].BoundingBox, position))
                        return;
                }
            }

            if (token.IsCancellationRequested)
                return;
        }

        var subsector = world.BspTree.Subsectors[nodeIndex & BspNodeCompact.SubsectorMask];
        var lineArray = world.StructLines.Data;
        uint smallerAngle;
        uint largerAngle;
        fixed (SubsectorSegment* startEdge = &world.BspTree.Segments.Data[subsector.SegIndex])
        {
            SubsectorSegment* edge = startEdge;
            for (int i = 0; i < subsector.SegCount; i++, edge++)
            {
                if (edge->LineId == -1)
                    continue;

                ref var line = ref lineArray[edge->LineId];
                if (m_hitLines.Get(edge->LineId))
                    continue;

                m_hitLines.Set(line.Id, true);

                if (line.BackSector == null && line.Segment.PerpDot(position) > 0)
                    continue;

                (smallerAngle, largerAngle) = m_viewClipper.GetAngles(line.Segment.Start, line.Segment.End);
                if (m_viewClipper.InsideAnyRange(smallerAngle, largerAngle))
                    continue;

                if (line.BackCeilingPlane == null || RenderBlock.IsBlocked(world, edge, ref line))
                    m_viewClipper.AddLine(smallerAngle, largerAngle);

                if ((line.Flags & StructLineFlags.SeenForAutomap) != 0)
                    continue;

                if (!m_frustumPlanes.PointInFrustum(line.Segment.Start.X, line.Segment.Start.Y) &&
                    !m_frustumPlanes.PointInFrustum(line.Segment.End.X, line.Segment.End.Y))
                    continue;

                line.Flags |= StructLineFlags.SeenForAutomap;
                line.Line.DataChanges |= LineDataTypes.Automap;
            }
        }
    }

    private bool Occluded(in Box2D box, in Vec2D position)
    {
        if (!m_frustumPlanes.BoxInFront(box))
            return true;

        box.GetSpanningEdge(position, out var first, out var second);
        return m_viewClipper.InsideAnyRange(first, second);
    }
}
