using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Automap;
using Helion.Util;
using System.Collections.Generic;
using System.Diagnostics;

namespace Helion.World.Entities.Players;

public record struct PlayerTracerInfo(int Id, int Gametick, int Ticks, Vec3F Color, AutomapColor? AutomapColor)
{
    public Seg3D LookPath;
    public Seg3D AimPath;
    public readonly List<Seg3D> Tracers = new();
}

/// <summary>
/// A tracker of player tracers and tracing info that can be used in debugging.
/// </summary>
public class PlayerTracers
{
    private int Id;
    public const int MaxTracers = 4 * (int)Constants.TicksPerSecond;
    public const int TracerRenderTicks = 4 * (int)Constants.TicksPerSecond;
    public static readonly Vec3F TracerColor = (1, 1, 1);
    public static readonly Vec3F LookColor = (0, 1, 1);
    public static readonly Vec3F AimColor = (0, 1, 0);

    public readonly LinkedList<PlayerTracerInfo> Tracers = new();

    private PlayerTracerInfo GetOrCreateTracerInfo(int gametick, Vec3F color, int ticks = TracerRenderTicks, AutomapColor? automapColor = null)
    {
        PlayerTracerInfo? info = FindTracer(color, ticks);
        // If there's no items, then make the first one.
        if (info == null)
        {
            info = new(++Id, gametick, ticks, color, automapColor);
            Tracers.AddFirst(info.Value);
            return info.Value;
        }

        // If we can't find the latest gametick, then we make a new one.
        Debug.Assert(gametick >= info.Value.Gametick, "Trying to add an older gametick, should only be adding current or newer tracers");
        if (info.Value.Gametick != gametick)
        {
            info = new(++Id, gametick, ticks, color, automapColor);
            Tracers.AddFirst(info.Value);
        }

        // We don't want to keep growing forever. In the future, use the data cache.
        while (Tracers.Count > MaxTracers)
            Tracers.RemoveLast();

        return info.Value;
    }

    private PlayerTracerInfo? FindTracer(Vec3F color, int ticks)
    {
        var node = Tracers.First;
        while (node != null)
        {
            if (node.Value.Color == color && node.Value.Ticks == ticks)
                return node.Value;
            node = node.Next;
        }
        return null;
    }

    public void AddLookPath(Vec3D start, double yaw, double pitch, double distance, int gametick)
    {
        Vec3D dir = Vec3D.UnitSphere(yaw, pitch);
        Vec3D end = start + (dir * distance);

        PlayerTracerInfo info = GetOrCreateTracerInfo(gametick, TracerColor);
        info.LookPath = (start, end);
    }

    public void AddAutoAimPath(Vec3D start, double yaw, double pitch, double distance, int gametick)
    {
        Vec3D dir = Vec3D.UnitSphere(yaw, pitch);
        Vec3D end = start + (dir * distance);

        PlayerTracerInfo info = GetOrCreateTracerInfo(gametick, AimColor);
        info.AimPath = (start, end);
    }

    public int AddTracer(Seg3D path, int gametick, Vec3F color, int ticks = TracerRenderTicks, AutomapColor? automapColor = null)
    {
        PlayerTracerInfo info = GetOrCreateTracerInfo(gametick, color, ticks, automapColor);
        info.Tracers.Add(path);
        return info.Id;
    }

    public void RemoveTracer(int id)
    {
        var node = Tracers.First;
        while (node != null)
        {
            if (node.Value.Id == id)
            {
                Tracers.Remove(node);
                break;
            }
            node = node.Next;
        }
    }
}
