using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Shared.World.ViewClipping;

namespace Helion.Tests.Unit.Render.Shared.World.ViewClipping;

public static class ViewClipperExtensions
{
    public static uint GetDiamondAngle(this ViewClipper viewClipper, Vec2D pos)
    {
        return ViewClipper.ToDiamondAngle(viewClipper.Center.X, viewClipper.Center.Y, pos.X, pos.Y);
    }

    public static uint ToDiamondAngle(Vec2D start, Vec2D end)
    {
        return ViewClipper.ToDiamondAngle(start.X, start.Y, end.X, end.Y);
    }
}
