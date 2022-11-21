using System;

namespace Helion.World.Geometry.Sides;

[Flags]
public enum SideDataTypes
{
    None = 0,
    UpperTexture = 1,
    MiddleTexture = 2,
    LowerTexture = 4
}
