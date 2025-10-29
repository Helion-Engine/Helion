using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.Util;
using Helion.World.Geometry.Lines;
using System;

namespace Helion.World.Special.Specials;

public struct ScrollSpeeds
{
    public Vec2D? ScrollSpeed;
    public Vec2D? CarrySpeed;
}

public static class ScrollUtil
{
    // Scrolling speeds from WinMBF.
    // Credit to Lee Killough et al.
    public static ScrollSpeeds GetScrollLineSpeed(Line line, ZDoomScroll flags, ZDoomPlaneScrollType type)
    {
        Vec2D diff;
        if ((flags & ZDoomScroll.Line) != 0)
        {
            diff = line.Segment.Delta;
            diff /= 32;
        }
        else
        {
            // Not sure why but ZDoom wiki indicates that 128 means no scrolling...
            diff.X = (line.Args.Arg3 - 128) / 32.0;
            diff.Y = (line.Args.Arg4 - 128) / 32.0;
        }

        return GetScrollSpeeds(diff, type);
    }

    public static ScrollSpeeds GetScrollSpeeds(Vec2D speed, ZDoomPlaneScrollType type)
    {
        ScrollSpeeds scrollSpeeds = new();
        if (type == ZDoomPlaneScrollType.Scroll || type == ZDoomPlaneScrollType.ScrollAndCarry)
            scrollSpeeds.ScrollSpeed = speed;

        if (type == ZDoomPlaneScrollType.Carry || type == ZDoomPlaneScrollType.ScrollAndCarry)
        {
            speed *= 0.09375;
            scrollSpeeds.CarrySpeed = speed;
        }

        if (scrollSpeeds.ScrollSpeed.HasValue)
            scrollSpeeds.ScrollSpeed = new Vec2D(-scrollSpeeds.ScrollSpeed.Value.X, scrollSpeeds.ScrollSpeed.Value.Y);

        return scrollSpeeds;
    }

    public static ScrollSpeeds GetScrollLineSpeed(Line from, Line to)
    {
        Vec2D fromDiff = (from.Segment.Delta) / 32;
        Vec2D toDiff = to.Segment.Delta;
        Vec2D toDiffOrig = toDiff;
        toDiff.X = Math.Abs(toDiff.X);
        toDiff.Y = Math.Abs(toDiff.Y);

        if (toDiff.Y > toDiff.X)
        {
            double save = toDiff.Y;
            toDiff.Y = toDiff.X;
            toDiff.X = save;
        }

        double d = toDiff.X / Math.Sin(Math.Atan2(toDiff.Y, toDiff.X) + MathHelper.HalfPi);
        toDiff.X = -(((fromDiff.Y * toDiffOrig.Y) + (fromDiff.X * toDiffOrig.X)) / d);
        toDiff.Y = -(((fromDiff.X * toDiffOrig.Y) - (fromDiff.Y * toDiffOrig.X)) / d);

        return new ScrollSpeeds() { ScrollSpeed = toDiff };
    }
}
