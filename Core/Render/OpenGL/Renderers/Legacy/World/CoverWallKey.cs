using Helion.World.Geometry.Walls;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

readonly struct CoverWallKey(int sideId, WallLocation location)
{
    public readonly int SideId = sideId;
    public readonly WallLocation Location = location;

    public override int GetHashCode()
    {
        return SideId + (int)Location * 131072;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is CoverWallKey key)
            return key.SideId == SideId && key.Location == Location;
        return false;
    }
}
