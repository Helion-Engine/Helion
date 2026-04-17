using Helion.World.Geometry.Sectors;
using Helion.World.Special.Specials;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.Util;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;

namespace Helion.World.Static;

public class StaticDataApplier
{
    private static bool IsLoading;

    public static void DetermineStaticData(WorldBase world)
    {
        IsLoading = true;
        for (int i = 0; i < world.Lines.Count; i++)
            DetermineStaticSectorLine(world, world.Lines[i]);

        foreach (var special in world.SpecialManager.GetSpecials())
        {
            if (special is ScrollSpecial scrollSpecial)
            {
                if (scrollSpecial.SectorPlane != null)
                {
                    SetSectorDynamic(world, scrollSpecial.SectorPlane.Sector, scrollSpecial.SectorPlane.Facing.ToSectorPlanes(), SectorDynamic.Scroll);
                }
                else if (scrollSpecial.Line != null && scrollSpecial.Speed.Y != 0)
                {
                    scrollSpecial.Line.Front.Dynamic |= SectorDynamic.ScrollY;
                    if (scrollSpecial.Line.Back != null)
                        scrollSpecial.Line.Back.Dynamic |= SectorDynamic.ScrollY;
                }                    
            }
        }

        for (int i = 0; i < world.Sectors.Count; i++)
            world.RenderBlockmap.Link(world, world.Sectors[i]);

        IsLoading = false;
    }

    private static void DetermineStaticSectorLine(WorldBase world, Line line)
    {
        CheckFloodFill(world, line);

        //if (line.Back != null && line.Alpha < 1)
        //{
        //    line.Front.Dynamic |= SectorDynamic.Alpha;
        //    line.Back.Dynamic |= SectorDynamic.Alpha;
        //    world.RenderBlockmap.LinkDynamicSide(line.Front);
        //    if (line.Front.Sector != line.Back.Sector)
        //        world.RenderBlockmap.LinkDynamicSide(line.Back);
        //    return;
        //}

        if (line.Front.ScrollData != null)
        {
            line.Front.Dynamic |= SectorDynamic.Scroll;
            world.RenderBlockmap.LinkDynamicSide(line.Front);
        }

        if (line.Back != null && line.Back.ScrollData != null)
        {
            line.Back.Dynamic |= SectorDynamic.Scroll;
            world.RenderBlockmap.LinkDynamicSide(line.Back);
        }
    }

    public static void CheckFloodFill(IWorld world, Line line)
    {
        if (line.Back == null)
            return;

        var frontSector = line.Front.Sector.GetRenderSector(TransferHeightView.Middle);
        var backSector = line.Back.Sector.GetRenderSector(TransferHeightView.Middle);
        SetFloodFillSide(world, line.Front, line.Back, frontSector, backSector);
        SetFloodFillSide(world, line.Back, line.Front, backSector, frontSector);
    }

    public static bool ShouldFloodLower(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector)
    {
        return facingSide.Lower.TextureHandle <= Constants.NullCompatibilityTextureIndex &&
            (facingSector.Floor.Z < otherSector.Floor.Z || facingSector.Floor.PrevZ < otherSector.Floor.PrevZ);
    }

    public static bool ShouldFloodUpper(IWorld world, Side facingSide, Side otherSide, Sector facingSector, Sector otherSector)
    {
        return facingSide.Upper.TextureHandle <= Constants.NullCompatibilityTextureIndex &&
            (facingSector.Ceiling.Z > otherSector.Ceiling.Z || facingSector.Ceiling.PrevZ > otherSector.Ceiling.PrevZ) &&
            GeometryRenderer.UpperIsVisibleOrFlood(world.ArchiveCollection.TextureManager, facingSide, otherSide, facingSector, otherSector, out _);
    }

    public static void SetFloodFillSide(IWorld world, Side facingSide, Side otherSide, Sector facingSector, Sector otherSector)
    {
        if (ShouldFloodLower(facingSide, otherSide, facingSector, otherSector))
            facingSide.FloodTextures |= SideTexture.Lower;
        else
            facingSide.FloodTextures &= ~SideTexture.Lower;

        if (ShouldFloodUpper(world, facingSide, otherSide, facingSector, otherSector))
            facingSide.FloodTextures |= SideTexture.Upper;
        else
            facingSide.FloodTextures &= ~SideTexture.Upper;
    }

    public static void SetSectorDynamic(WorldBase world, Sector sector, SectorPlanes face, SectorDynamic sectorDynamic)
    {
        if (IsLoading && sectorDynamic == SectorDynamic.Movement)
            return;

        if ((face & SectorPlanes.Floor) != 0)
            sector.Floor.Dynamic |= sectorDynamic;
        if ((face & SectorPlanes.Ceiling) != 0)
            sector.Ceiling.Dynamic |= sectorDynamic;

        if (sector.Sector3D == null)
        {
            if (ShouldLink(sector, sectorDynamic))
                world.RenderBlockmap.LinkDynamic(world, sector);
        }
        else if (ShouldLink(sector.Sector3D.FakeSector, sectorDynamic))
        {
            world.RenderBlockmap.LinkDynamic(world, sector.Sector3D);
        }

        if (sectorDynamic == SectorDynamic.Movement)
            SetSectorDynamicMovement(sector);
        else if (sectorDynamic == SectorDynamic.TransferHeights)
            SetSectorTransferHeights(sector);
    }

    private static bool ShouldLink(Sector sector, SectorDynamic sectorDynamic)
    {
        return sector.BlockmapNodes.Length == 0 && (sectorDynamic & (SectorDynamic.Movement | SectorDynamic.Scroll)) != 0;
    }

    private static void SetSectorTransferHeights(Sector sector)
    {
        for (int i = 0; i < sector.Lines.Length; i++)
        {
            var line = sector.Lines[i];
            if (line.Front.Sector.Id == sector.Id)
                line.Front.Dynamic |= SectorDynamic.TransferHeights;
            if (line.Back != null && line.Back.Sector == sector)
                line.Back.Dynamic |= SectorDynamic.TransferHeights;
        }
    }

    private static void SetSectorDynamicMovement(Sector sector)
    {
        for (int i = 0; i < sector.Lines.Length; i++)
            SetDynamicMovement(sector.Lines[i]);
    }

    public static void ClearSectorDynamicMovement(IWorld world, SectorPlane plane)
    {
        plane.Dynamic &= ~SectorDynamic.Movement;

        // Floor and ceiling can move independently so don't clear it yet.
        if (plane.Sector.IsMoving || (plane.Dynamic & SectorDynamic.TransferHeights) != 0 || (WorldStatic.Sector3D && plane.Sector.Sector3D != null && plane.Sector.Sector3D.ControlSector.IsMoving))
            return;
                
        if ((plane.Sector.Floor.Dynamic & SectorDynamic.Scroll) == 0 && (plane.Sector.Ceiling.Dynamic & SectorDynamic.Scroll) == 0)
            plane.Sector.UnlinkFromWorld(world);

        for (int i = 0; i < plane.Sector.Lines.Length; i++)
            ClearDynamicMovement(plane.Sector.Lines[i]);
    }

    private static void SetDynamicMovement(Line line)
    {
        if (line.Back != null)
            line.Back.Dynamic |= SectorDynamic.Movement;

        line.Front.Dynamic |= SectorDynamic.Movement;
    }

    private static void ClearDynamicMovement(Line line)
    {
        if (line.Front.Sector.IsMoving)
            return;

        if (line.Back != null && line.Back.Sector.IsMoving)
            return;

        if (line.Back != null)
            line.Back.Dynamic &= ~SectorDynamic.Movement;

        line.Front.Dynamic &= ~SectorDynamic.Movement;
    }
}
