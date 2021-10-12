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
}

