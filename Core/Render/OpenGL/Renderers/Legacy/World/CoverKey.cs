using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Walls;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

readonly struct CoverKey(int key1, int key2) : IEquatable<CoverKey>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CoverKey MakeCoverWallKey(int sideId, WallLocation location, bool oneSided) => new(sideId, (int)location * (oneSided ? 1 : 2));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CoverKey MakeFlatKey(int sectorId, SectorPlaneFace plane) => new(sectorId, (int)plane);

    public readonly int Key1 = key1;
    public readonly int Key2 = key2;

    public override int GetHashCode()
    {
        return Key1 + Key2 * 131072;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is CoverKey key)
            return key.Key1 == Key1 && key.Key2 == Key2;
        return false;
    }

    public bool Equals(CoverKey other)
    {
        return other.Key1 == Key1 && other.Key2 == Key2;
    }
}
