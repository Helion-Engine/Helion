using System;

namespace Helion.World.Geometry.Lines;

[Flags]
public enum LineDataTypes
{
    Activated = 1,
    Texture = 2,
    Automap = 4,
    Args = 8,
    Alpha = 16,
    EverActivated = 32,
    BlockFlags = 64,
    BlockSound = 128,
    Special = 256,
}
