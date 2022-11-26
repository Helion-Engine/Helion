using System;
using Helion.Geometry.Vectors;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Automap;

public enum AutomapColor
{
    White,
    Gray,
    Red,
    Yellow,
    Blue,
    Purple,
    Green,
    LightBlue
}

public static class AutomapColorHelper
{
    public static Vec3F ToColor(this AutomapColor color)
    {
        return color switch
        {
            AutomapColor.White => (1, 1, 1),
            AutomapColor.Gray => (0.5f, 0.5f, 0.5f),
            AutomapColor.Red => (1, 0, 0),
            AutomapColor.Yellow => (1, 1, 0),
            AutomapColor.Blue => (0, 0, 1),
            AutomapColor.Purple => (0.6f, 0.25f, 0.8f),
            AutomapColor.Green => (0, 1, 0),
            AutomapColor.LightBlue => (0.34f, 0.8f, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };
    }
}
