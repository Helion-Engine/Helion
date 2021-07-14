using System;

namespace Helion.World.Geometry.Sectors
{
    [Flags]
    public enum SectorDataTypes
    {
        FloorZ = 1,
        CeilingZ = 2,
        Light = 4,
        FloorTexture = 8,
        CeilingTexture = 16,
        SectorSpecialType = 32,
        MovementLocked = 64
    }
}
