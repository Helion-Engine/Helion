using System;
using System.Runtime.CompilerServices;

namespace Helion.World.Geometry.Sectors;

public enum SectorPlaneFace
{
    Floor,
    Ceiling,
}

[Flags]
public enum SectorPlanes
{
    None = 0,
    Floor = 1,
    Ceiling = 2
}

public static class SectorPlaneExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SectorPlanes ToSectorPlanes(this SectorPlaneFace face) => (SectorPlanes)(face + 1); 
}
