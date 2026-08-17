using Helion.Util;
using Helion.Util.Assertion;
using Helion.World;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;

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

    public static bool IsBlocked(Line line)
    {
        if (line.Back == null)
            return true;

        // TODO This can be smarter. This is just to allow rendering tricks for invisible platforms.
        if (line.Front.Sector.TransferHeights != null || line.Back.Sector.TransferHeights != null)
            return false;

        if (line.Back.Sector.Ceiling.Z <= line.Back.Sector.Floor.Z)
        {
            if (line.Back.Sector.Ceiling.Z < line.Front.Sector.Floor.Z)
                return line.Front.Upper.TextureHandle > Constants.NullCompatibilityTextureIndex;

            if (line.Back.Sector.Ceiling.Z > line.Front.Sector.Floor.Z)
                return line.Front.Lower.TextureHandle > Constants.NullCompatibilityTextureIndex;

            return true;
        }

        if (line.Front.Sector.Ceiling.Z <= line.Front.Sector.Floor.Z)
        {
            if (line.Back.Sector.Ceiling.Z < line.Back.Sector.Floor.Z)
                return line.Back.Upper.TextureHandle > Constants.NullCompatibilityTextureIndex;

            if (line.Front.Sector.Ceiling.Z > line.Back.Sector.Floor.Z)
                return line.Back.Lower.TextureHandle > Constants.NullCompatibilityTextureIndex;

            return true;
        }

        return false;
    }

    public static bool IsBlocked(in StructLine line)
    {
        if (line.BackCeilingPlane != null && line.BackFloorPlane != null && line.BackCeilingPlane.Z <= line.BackFloorPlane.Z)
        {
            if (line.BackCeilingPlane.Z < line.FrontFloorPlane.Z)
                return line.Line.Front.Upper.TextureHandle > Constants.NullCompatibilityTextureIndex;

            if (line.BackCeilingPlane.Z > line.FrontFloorPlane.Z)
                return line.Line.Front.Lower.TextureHandle > Constants.NullCompatibilityTextureIndex;

            return true;
        }

        if (line.BackFloorPlane != null && line.Line.Back != null && line.FrontCeilingPlane.Z <= line.FrontFloorPlane.Z)
        {
            if (line.FrontCeilingPlane.Z < line.BackFloorPlane.Z)
                return line.Line.Back.Upper.TextureHandle > Constants.NullCompatibilityTextureIndex;

            if (line.FrontCeilingPlane.Z > line.BackFloorPlane.Z)
                return line.Line.Back.Lower.TextureHandle > Constants.NullCompatibilityTextureIndex;

            return true;
        }

        return false;
    }

    public static bool IsBlocked(IWorld world, ref SubsectorSegment edge, ref StructLine line, bool onFrontSide)
    {
        if (line.BackCeilingPlane == null || line.BackFloorPlane == null || edge.SideId == -1)
            return true;

        SectorPlane frontFloorPlane;
        SectorPlane backFloorPlane;
        SectorPlane frontCeilingPlane;
        SectorPlane backCeilingPlane;
        if (onFrontSide)
        {
            frontFloorPlane = line.FrontFloorPlane;
            backFloorPlane = line.BackFloorPlane;
            frontCeilingPlane = line.FrontCeilingPlane;
            backCeilingPlane = line.BackCeilingPlane;
        }
        else
        {
            frontFloorPlane = line.BackFloorPlane;
            backFloorPlane = line.FrontFloorPlane;
            frontCeilingPlane = line.BackCeilingPlane;
            backCeilingPlane = line.FrontCeilingPlane;
        }

        if (backCeilingPlane.Z <= frontFloorPlane.Z)
            return world.Sides[edge.SideId].Upper.TextureHandle > Constants.NullCompatibilityTextureIndex;

        if (frontCeilingPlane.Z <= backFloorPlane.Z)
            return world.Sides[edge.SideId].Lower.TextureHandle > Constants.NullCompatibilityTextureIndex;

        if (backCeilingPlane.Z <= backFloorPlane.Z)
        {
            if (backCeilingPlane.Z < frontCeilingPlane.Z)
                return world.Sides[edge.SideId].Upper.TextureHandle > Constants.NullCompatibilityTextureIndex;
            if (backFloorPlane.Z < frontFloorPlane.Z)
                return world.Sides[edge.SideId].Upper.TextureHandle > Constants.NullCompatibilityTextureIndex;
        }

        return false;
    }
}
