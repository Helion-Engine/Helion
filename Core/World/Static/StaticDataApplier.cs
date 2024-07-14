using Helion.World.Geometry.Sectors;
using Helion.World.Special.Specials;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.Resources;
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

        if (world.Config.Developer.FloodOpposing)
        {
            for (int i = 0; i < world.Lines.Count; i++)
            {
                var line = world.Lines[i];
                if (line.Back == null)
                    continue;

                SetOpposingFlood(line.Front);
                SetOpposingFlood(line.Back);
            }
        }

        foreach (var special in world.SpecialManager.GetSpecials())
        {
            if (special is ScrollSpecial scrollSpecial && scrollSpecial.SectorPlane != null)
                SetSectorDynamic(world, scrollSpecial.SectorPlane.Sector, scrollSpecial.SectorPlane.Facing.ToSectorPlanes(), SectorDynamic.Scroll);
        }

        for (int i = 0; i < world.Sectors.Count; i++)
            DetermineStaticSector(world, world.Sectors[i], world.TextureManager);

        IsLoading = false;
    }

    private static void DetermineStaticSector(WorldBase world, Sector sector, TextureManager textureManager)
    {
        if (sector.TransferHeights == null)
            return;

        bool save = IsLoading;
        IsLoading = false;
        SetSectorDynamic(world, sector, SectorPlanes.Floor | SectorPlanes.Ceiling, SectorDynamic.TransferHeights);
        IsLoading = save;
    }

    private static void DetermineStaticSectorLine(WorldBase world, Line line)
    {
        CheckFloodFill(world, line);

        if (line.Back != null && line.Alpha < 1)
        {
            line.Front.Dynamic |= SectorDynamic.Alpha;
            line.Back.Dynamic |= SectorDynamic.Alpha;
            world.RenderBlockmap.LinkDynamicSide(line.Front);
            if (line.Front.Sector != line.Back.Sector)
                world.RenderBlockmap.LinkDynamicSide(line.Back);
            return;
        }

        if (line.Front.ScrollData != null)
        {
            line.Front.Dynamic |= SectorDynamic.Scroll;
            world.RenderBlockmap.LinkDynamicSide(line.Front);
        }

        if (line.Back != null && line.Back.ScrollData != null)
        {
            line.Front.Dynamic |= SectorDynamic.Scroll;
            world.RenderBlockmap.LinkDynamicSide(line.Back);
        }
    }

    private static void SetOpposingFlood(Side side)
    {
        if ((side.FloodTextures & SideTexture.Lower) != 0 && !side.PartnerSide!.Sector.Flood)
            side.PartnerSide.Sector.FloodOpposingFloor = true;
        if ((side.FloodTextures & SideTexture.Upper) != 0 && !side.PartnerSide!.Sector.Flood)
            side.PartnerSide.Sector.FloodOpposingCeiling = true;
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

    public static void SetFloodFillSide(IWorld world, Side facingSide, Side otherSide, Sector facingSector, Sector otherSector)
    {
        if (facingSide.Lower.TextureHandle <= Constants.NullCompatibilityTextureIndex && 
            (facingSector.Floor.Z < otherSector.Floor.Z || facingSector.Floor.PrevZ < otherSector.Floor.PrevZ))
            facingSide.FloodTextures |= SideTexture.Lower;
        else
            facingSide.FloodTextures &= ~SideTexture.Lower;

        if (facingSide.Upper.TextureHandle <= Constants.NullCompatibilityTextureIndex && 
            (facingSector.Ceiling.Z > otherSector.Ceiling.Z || facingSector.Ceiling.PrevZ > otherSector.Ceiling.PrevZ) &&
            GeometryRenderer.UpperIsVisibleOrFlood(world.ArchiveCollection.TextureManager, facingSide, otherSide, facingSector, otherSector))
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

        if (sector.BlockmapNodes.Length == 0 && (sectorDynamic == SectorDynamic.TransferHeights || sectorDynamic == SectorDynamic.Movement || sectorDynamic == SectorDynamic.Scroll))
            world.RenderBlockmap.Link(world, sector);

        if (sectorDynamic == SectorDynamic.Movement)
            SetSectorDynamicMovement(world, sector);
        else if (sectorDynamic == SectorDynamic.TransferHeights)
            SetSectorTransferHeights(sector);
    }

    private static void SetSectorTransferHeights(Sector sector)
    {
        for (int i = 0; i < sector.Lines.Count; i++)
        {
            var line = sector.Lines[i];
            if (line.Front.Sector.Id == sector.Id)
                line.Front.Dynamic |= SectorDynamic.TransferHeights;
            if (line.Back != null && line.Back.Sector == sector)
                line.Back.Dynamic |= SectorDynamic.TransferHeights;
        }
    }

    private static void SetSectorDynamicMovement(WorldBase world, Sector sector)
    {
        for (int i = 0; i < sector.Lines.Count; i++)
            SetDynamicMovement(sector.Lines[i]);
    }

    public static void ClearSectorDynamicMovement(IWorld world, SectorPlane plane)
    {
        plane.Dynamic &= ~SectorDynamic.Movement;

        // Floor and ceiling can move independently so don't clear it yet.
        if (plane.Sector.IsMoving || (plane.Dynamic & SectorDynamic.TransferHeights) != 0)
            return;

        plane.Sector.UnlinkFromWorld(world);

        bool floor = plane.Facing == SectorPlaneFace.Floor;
        bool ceiling = plane.Facing == SectorPlaneFace.Ceiling;

        for (int i = 0; i < plane.Sector.Lines.Count; i++)
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
