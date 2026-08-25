using Helion.Util;
using Helion.Util.Assertion;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using System.Runtime.CompilerServices;

namespace Helion.Render;

// Check null texture handle for rendering tricks. Not blocked if missing texture.
// Works around issue the original game had not adding blocking lines to view clipper.
// E.g. TNT Map02 - see through window that opens as a door
public static class RenderBlock
{
    public static bool IsSkyBlocked(Line line)
    {
        Assert.Precondition(line.Back != null, "Cannot create LineOpening with one sided line");

        // TODO This can be smarter. This is just to allow rendering tricks for invisible platforms.
        if (line.Front.Sector.TransferHeights != null || line.Back!.Sector.TransferHeights != null)
            return false;

        // Closed door check. This check isn't really correct, but is required for some old rendering tricks to work.
        if (line.Back!.Sector.Ceiling.Z <= line.Front.Sector.Floor.Z || line.Back.Sector.Floor.Z >= line.Front.Sector.Ceiling.Z)
            return true;

        return false;
    }

    public static bool IsBlocked(Line line, bool onFrontSide)
    {
        if (line.Back == null)
            return true;

        return onFrontSide ? IsBlocked(line.Front, line.Front.Sector.Floor, line.Back.Sector.Floor, line.Front.Sector.Ceiling, line.Back.Sector.Ceiling) :
            IsBlocked(line.Back, line.Back.Sector.Floor, line.Front.Sector.Floor, line.Back.Sector.Ceiling, line.Front.Sector.Ceiling);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBlocked(Side side, Sector frontSector, Sector backSector)
    {
        return IsBlocked(side, frontSector.Floor, backSector.Floor, frontSector.Ceiling, backSector.Ceiling);
    }

    public static bool IsBlocked(Side side, SectorPlane frontFloorPlane, SectorPlane backFloorPlane, SectorPlane frontCeilingPlane, SectorPlane backCeilingPlane)
    {
        if (frontFloorPlane.TransferHeights != null || backFloorPlane.TransferHeights != null)
            return false;

        if (backCeilingPlane.Z <= frontFloorPlane.Z)
            return side.Upper.TextureHandle > Constants.NullCompatibilityTextureIndex;

        if (frontCeilingPlane.Z <= backFloorPlane.Z)
            return side.Lower.TextureHandle > Constants.NullCompatibilityTextureIndex;

        if (backCeilingPlane.Z <= backFloorPlane.Z)
        {
            if (backCeilingPlane.Z < frontCeilingPlane.Z)
                return side.Upper.TextureHandle > Constants.NullCompatibilityTextureIndex;
            if (backFloorPlane.Z < frontFloorPlane.Z)
                return side.Upper.TextureHandle > Constants.NullCompatibilityTextureIndex;
        }
        return false;
    }
}
