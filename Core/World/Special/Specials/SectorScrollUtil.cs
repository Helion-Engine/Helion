using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.World.Geometry.Lines;

namespace Helion.World.Special.Specials
{    
    public struct SectorScrollSpeeds
    {
        public Vec2D? ScrollSpeed;
        public Vec2D? CarrySpeed;
    }

    public static class SectorScrollUtil
    {
        // Scrolling speeds from WinMBF.
        // Credit to Lee Killough et al.
        public static SectorScrollSpeeds GetScrollLineSpeed(Line line, ZDoomPlaneScroll flags, ZDoomPlaneScrollType type)
        {
            SectorScrollSpeeds scrollSpeeds = new();
            Vec2D diff;
            if (flags.HasFlag(ZDoomPlaneScroll.Line))
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
                scrollSpeeds.ScrollSpeed = diff * SpecialManager.VisualScrollFactor;

            if (type == ZDoomPlaneScrollType.Carry || type == ZDoomPlaneScrollType.ScrollAndCarry)
            {
                diff *= 0.09375;
                scrollSpeeds.CarrySpeed = diff;
            }

            return scrollSpeeds;
        }
    }
}
