using System;

namespace Helion.Layer.Worlds
{
    [Flags]
    public enum RenderHudOptions
    {
        Weapon = 1,
        Crosshair = 2,
        Hud = 4,
        Overlay = 8
    }
}
