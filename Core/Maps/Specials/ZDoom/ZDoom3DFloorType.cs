using System;

namespace Helion.Maps.Specials.ZDoom;

public enum ZDoom3DFloorType
{
    None = 0,
    Solid = 1,
    Swimmable = 2,
    NonSolid = 3,
}

[Flags]
public enum ZDoom3DFloorFlagsForType
{
    RenderInside = 4,
    VisibilityInvert = 16,
    ShootabilityInvert = 32
}

[Flags]
public enum ZDoom3DFloorFlags
{
    DisableLighting = 1,
    RestrictLighting = 2,
    Fog = 4,
    Model = 8,
    UseUpperTexture = 16,
    UserLowerTexture = 32,
    AdditiveTransparency = 64,
    Fade = 512,
    ResetAbove = 1024
}

public enum ZDoom3DFloorLightFlags
{
    ToNextTypeZero, // Extra light extends from ceiling of control sector down to top of another type 0 light
    ToControlFloor, // Extra light extends from ceiling down to the floor of the control sector.
    ToNextAny // Extra light extends from control sector's ceiling down to the top of another extra light.
}