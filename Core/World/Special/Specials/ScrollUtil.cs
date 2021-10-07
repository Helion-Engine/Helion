using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.Util;
using Helion.World.Geometry.Lines;
using System;

namespace Helion.World.Special.Specials
{    
    public struct ScrollSpeeds
    {
        public Vec2D? ScrollSpeed { get; set; }
        public Vec2D? CarrySpeed { get; set; }
    }

    public static class ScrollUtil
    {
        // Scrolling speeds from WinMBF.
        // Credit to Lee Killough et al.
        public static ScrollSpeeds GetScrollLineSpeed(Line line, ZDoomScroll flags, ZDoomPlaneScrollType type, double visualScrollFactor = 1.0)
        {
            ScrollSpeeds scrollSpeeds = new();
            Vec2D diff;
            if (flags.HasFlag(ZDoomScroll.Line))
            {
                diff = line.EndPosition - line.StartPosition;
                diff /= 32;
            }
            else
            {
                // Not sure why but ZDoom wiki indicates that 128 means no scrolling...
                diff.X = (line.Args.Arg3 - 128) / 32.0;
                diff.Y = (line.Args.Arg4 - 128) / 32.0;
            }

            if (type == ZDoomPlaneScrollType.Scroll || type == ZDoomPlaneScrollType.ScrollAndCarry)
                scrollSpeeds.ScrollSpeed = diff * visualScrollFactor;

            if (type == ZDoomPlaneScrollType.Carry || type == ZDoomPlaneScrollType.ScrollAndCarry)
            {
                diff *= 0.09375;
                scrollSpeeds.CarrySpeed = diff;
            }

            if (scrollSpeeds.ScrollSpeed.HasValue)
                scrollSpeeds.ScrollSpeed = new Vec2D(scrollSpeeds.ScrollSpeed.Value.X, -scrollSpeeds.ScrollSpeed.Value.Y);

            return scrollSpeeds;
        }

        public static ScrollSpeeds GetScrollLineSpeed(Line from, Line to)
        {
            Vec2D fromDiff = (from.EndPosition - from.StartPosition) / 32;
            Vec2D toDiff = to.EndPosition - to.StartPosition;
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
}
