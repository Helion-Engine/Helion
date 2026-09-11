using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Render;
using Helion.Render.Common.Shared;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World.ViewClipping;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Components;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Helion.World.Impl.SinglePlayer;

public class AutomapMarker(IConfig config) : IBspHeuristics
{
    private BitArray m_hitLines = new(0);
    private readonly Stopwatch m_stopwatch = new();
    private readonly ViewClipper m_viewClipper = new();
    private readonly RenderInfo m_renderInfo = new();
    private readonly OldCamera m_camera = new(default, default, 0, 0);
    private readonly Entity m_dummyEntity = new();
    private Thread? m_thread;
    private CancellationTokenSource m_cancelTasks = new();
    private IWorld m_world = null!;
    private FrustumPlanes m_frustumPlanes;
    private int m_subsectorCount;
    private int m_segCount;
    private int m_lineCount;
    private bool m_markLines;

    private readonly IConfig m_config = config;
    private readonly ConcurrentQueue<PlayerPosition> m_positions = new();
    
    public int LastProcessedId { get; private set; }
    public event EventHandler<PlayerPosition>? PositionProcessed;

    public bool Valid { get; private set; }
    public int SubsectorCount {  get; private set; }
    public int SegCount { get; private set; }
    public int LineCount { get; private set; }
    public int Microseconds { get; private set; }
    public long LastProcessedTimeStamp { get; private set; }
    public BspHeuristicInfo Info { get; } = new();

    public void Start(IWorld world)
    {
        if (m_thread != null)
            return;

        ClearData();

        LastProcessedId = -1;
        world.OnDestroying += World_OnDestroying;
        m_world = world;
        m_hitLines = new(world.Lines.Count);

        m_dummyEntity.Set(0, 0, 0, EntityDefinition.Default, default, 0, m_world.Sectors[0], m_world, default);

        m_thread = new Thread(() => AutomapTask(m_cancelTasks.Token))
        {
            IsBackground = true
        };
        m_thread.Start();
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
        if (m_thread == null)
            return;

        m_cancelTasks.Cancel();
        m_cancelTasks.Dispose();
        m_thread.Join();

        ClearData();

        m_cancelTasks = new CancellationTokenSource();
        m_thread = null;
    }

    private void ClearData()
    {
        m_positions.Clear();
        m_viewClipper.Clear();
    }

    public void AddPosition(Vec3D pos, Vec3D viewDirection, double angleRadians, double pitchRadians, int id, bool markLines = true)
    {
        if (m_config.Render.Mode.Value != AdaptiveRenderMode.Adaptive || m_positions.IsEmpty)
            m_positions.Enqueue(new PlayerPosition(pos, viewDirection, angleRadians, pitchRadians, id, markLines));
    }

    private void AutomapTask(CancellationToken token)
    {
        const int ClearCount = 5;
        int ticks = (int)(1000 / Constants.TicksPerSecond);
        while (true)
        {
            if (token.IsCancellationRequested)
                return;

            var viewport = GetViewport();
            m_stopwatch.Restart();

            while (m_world != null && m_positions.TryDequeue(out var pos))
            {
                // Don't let the queue fill up indefinitely when processing too slowly
                if (m_positions.Count > ClearCount)
                {
                    MaxHeuristics();
                    m_positions.Clear();
                }

                if (token.IsCancellationRequested)
                    return;

                m_markLines = pos.MarkLines;
                m_subsectorCount = 0;
                m_segCount = 0;
                m_lineCount = 0;
                m_viewClipper.Clear();
                m_viewClipper.Center = pos.Position.XY;
                m_hitLines.SetAll(false);

                SetFrustum(viewport, pos);
                MarkBspLineClips((uint)m_world.BspTree.Nodes.Length - 1, pos.Position.XY, m_world, token);
                LastProcessedId = pos.Id;
                PositionProcessed?.Invoke(this, pos);

                SetHeuristics();
            }

            m_stopwatch.Stop();
            if (m_stopwatch.ElapsedMilliseconds >= ticks)
                continue;

            if (m_config.Render.Mode.Value != AdaptiveRenderMode.Adaptive)
                Thread.Sleep(Math.Max(ticks - (int)m_stopwatch.ElapsedMilliseconds, 0));
            else
                Thread.Yield();
        }
    }

    private void MaxHeuristics()
    {
        Valid = false;
        Microseconds = int.MaxValue;
        SubsectorCount = int.MaxValue;
        SegCount = int.MaxValue;
        LineCount = int.MaxValue;
    }

    private void SetHeuristics()
    {
        Valid = true;
        LastProcessedTimeStamp = Stopwatch.GetTimestamp();
        Microseconds = (int)m_stopwatch.Elapsed.TotalMicroseconds;
        SubsectorCount = m_subsectorCount;
        SegCount = m_segCount;
        LineCount = m_lineCount;
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
            ref var node = ref world.BspTree.Nodes[nodeIndex];
            if (Occluded(node.BoundingBox, position))
                return;

            bool onRight = (node.SplitDelta.X * (position.Y - node.SplitStart.Y)) - (node.SplitDelta.Y * (position.X - node.SplitStart.X)) < 0;
            int front = *(byte*)&onRight;

            MarkBspLineClips(node.Children[front], position, world, token);

            nodeIndex = node.Children[front ^ 1];         

            if (token.IsCancellationRequested)
                return;
        }

        m_subsectorCount++;
        var subsector = world.BspTree.Subsectors[nodeIndex & BspNodeCompact.SubsectorMask];
        var lineArray = world.StructLines.Data;
        uint smallerAngle;
        uint largerAngle;

        for (int i = 0; i < subsector.SegCount; i++)
        {
            ref var edge = ref world.BspTree.Segments[subsector.SegIndex + i];
            if (edge.LineId == -1)
                continue;

            var dx = edge.End.X - edge.Start.X;
            var dy = edge.End.Y - edge.Start.Y;
            var front = (dx * (position.Y - edge.Start.Y)) - (dy * (position.X - edge.Start.X)) < 0;
            if (edge.BackSectorId == -1 && !front)
                continue;

            (smallerAngle, largerAngle) = m_viewClipper.GetAngles(edge.Start, edge.End);
            if (m_viewClipper.InsideAnyRange(smallerAngle, largerAngle))
                continue;

            var side = m_world.Sides[edge.SideId];
            if (edge.BackSectorId == -1  || RenderBlock.IsBlocked(side, m_world.Sectors[edge.FrontSectorId], m_world.Sectors[edge.BackSectorId]))
                m_viewClipper.AddLine(smallerAngle, largerAngle);

            m_segCount++;

            if (m_hitLines.Get(edge.LineId))
                continue;

            m_hitLines.Set(edge.LineId, true);

            ref var line = ref lineArray[edge.LineId];
            if (!m_frustumPlanes.PointInFrustum(line.Segment.Start.X, line.Segment.Start.Y) &&
                !m_frustumPlanes.PointInFrustum(line.Segment.End.X, line.Segment.End.Y))
                continue;

            m_lineCount++;
            if ((line.Flags & StructLineFlags.SeenForAutomap) != 0)
                continue;

            line.Flags |= StructLineFlags.SeenForAutomap;
            line.Line.DataChanges |= LineDataTypes.Automap;
        }        
    }

    private bool Occluded(in Box2D box, in Vec2D position)
    {
        if (box.Contains(position))
            return false;

        if (!m_frustumPlanes.BoxInFront(box))
            return true;

        box.GetSpanningEdge(position, out var x1, out var y1, out var x2, out var y2);
        return m_viewClipper.InsideAnyRange(x1, y1, x2, y2);
    }
}
