using System;
using GlmSharp;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Automap
{
    public enum AutomapColor
    {
        White, 
        Gray,
        Red,
        Yellow,
        Blue,
    }

    public static class AutomapColorHelper
    {
        public static vec3 ToColor(this AutomapColor color)
        {
            return color switch
            {
                AutomapColor.White => new vec3(1, 1, 1),
                AutomapColor.Gray => new vec3(0.5f, 0.5f, 0.5f),
                AutomapColor.Red => new vec3(1, 0, 0),
                AutomapColor.Yellow => new vec3(1, 1, 0),
                AutomapColor.Blue => new vec3(0, 0, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
            };
        }
    }
}
