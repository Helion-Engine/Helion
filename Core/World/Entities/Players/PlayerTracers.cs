using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace Helion.World.Entities.Players;

public record PlayerTracerInfo(int Gametick)
{
    public Seg3D LookPath;
    public Seg3D AimPath;
    public readonly List<Seg3D> Tracers = new();
}

/// <summary>
/// A tracker of player tracers and tracing info that can be used in debugging.
/// </summary>
public class PlayerTracers : IEnumerable<PlayerTracerInfo>
{
    public const int MaxTracers = 4 * (int)Constants.TicksPerSecond;
    public const int TracerRenderTicks = 4 * (int)Constants.TicksPerSecond;
    public static readonly Vec3F TracerColor = (1, 1, 1);
    public static readonly Vec3F LookColor = (0, 1, 1);
    public static readonly Vec3F AimColor = (0, 1, 0);

    private readonly LinkedList<PlayerTracerInfo> m_tracers = new();

    private PlayerTracerInfo GetOrCreateTracerInfo(int gametick)
    {
        PlayerTracerInfo? info = m_tracers.First?.Value;

        // If there's no items, then make the first one.
        if (info == null)
        {
            info = new(gametick);
            m_tracers.AddFirst(info);
            return info;
        }

        // If we can't find the latest gametick, then we make a new one.
        Debug.Assert(gametick >= info.Gametick, "Trying to add an older gametick, should only be adding current or newer tracers");
        if (info.Gametick != gametick)
        {
            info = new(gametick);
            m_tracers.AddFirst(info);
        }

        // We don't want to keep growing forever. In the future, use the data cache.
        while (m_tracers.Count > MaxTracers)
            m_tracers.RemoveLast();

        return info;
    }

    public void AddLookPath(Vec3D start, double yaw, double pitch, double distance, int gametick)
    {
        Vec3D dir = Vec3D.UnitSphere(yaw, pitch);
        Vec3D end = start + (dir * distance);

        PlayerTracerInfo info = GetOrCreateTracerInfo(gametick);
        info.LookPath = (start, end);
    }

    public void AddAutoAimPath(Vec3D start, double yaw, double pitch, double distance, int gametick)
    {
        Vec3D dir = Vec3D.UnitSphere(yaw, pitch);
        Vec3D end = start + (dir * distance);

        PlayerTracerInfo info = GetOrCreateTracerInfo(gametick);
        info.AimPath = (start, end);
    }

    public void AddTracer(Seg3D path, int gametick)
    {
        PlayerTracerInfo info = GetOrCreateTracerInfo(gametick);
        info.Tracers.Add(path);
    }

    public IEnumerator<PlayerTracerInfo> GetEnumerator() => ((IEnumerable<PlayerTracerInfo>)m_tracers).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)m_tracers).GetEnumerator();
}
