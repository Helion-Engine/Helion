using System;

namespace Helion.World.Geometry.Sectors;

[Flags]
public enum SectorDataTypes
{
    None = 0,
    FloorZ = 1,
    CeilingZ = 2,
    Light = 4,
    FloorTexture = 8,
    CeilingTexture = 16,
    SectorSpecialType = 32,
    MovementLocked = 64,
    Offset = 128,
    SectorEffect = 256,
    SkyTexture = 512,
    TransferHeights = 1024,
    Friction = 2048,
    KillEffect = 4096,
    ColorMap = 8192
}
