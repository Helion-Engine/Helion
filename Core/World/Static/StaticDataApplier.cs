using Helion.Util.Configs.Components;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.Specials;
using Helion.World.Special;
using System;
using System.Collections.Generic;
using Helion.Maps.Specials;
using Helion.World.Entities.Definition.States;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Special.Switches;
using Helion.Resources;
using Helion.Util;

namespace Helion.World.Static;

public class StaticDataApplier
{
    const SideTexture AllWallTypes = SideTexture.Upper | SideTexture.Middle | SideTexture.Lower;
    const SideTexture MiddleLower = SideTexture.Middle | SideTexture.Lower;
    const SideTexture MiddleUpper = SideTexture.Middle | SideTexture.Upper;

    private static bool IsLoading;

    public static void DetermineStaticData(WorldBase world)
    {
        IsLoading = true;
        for (int i = 0; i < world.Lines.Count; i++)
            DetermineStaticSectorLine(world, world.Lines[i]);

        foreach (var special in world.SpecialManager.GetSpecials())
        {
            if (special is SectorSpecialBase sectorSpecial)
            {
                SetSectorDynamic(world, sectorSpecial.Sector, true, true, SectorDynamic.Light);
            }
            else if (special is ScrollSpecial scrollSpecial && scrollSpecial.SectorPlane != null)
            {
                bool floor = scrollSpecial.SectorPlane.Facing == SectorPlaneFace.Floor;
                SetSectorDynamic(world, scrollSpecial.SectorPlane.Sector, floor, !floor, SectorDynamic.Scroll);
            }
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
        SetSectorDynamic(world, sector, true, true, SectorDynamic.TransferHeights);
        IsLoading = save;
    }

    private static void DetermineStaticSectorLine(WorldBase world, Line line)
    {
        // Hack for now until we have a better solution.
        line.MarkSeenOnAutomap();

        CheckFloodFill(world, line);

        if (line.Back != null && line.Alpha < 1)
        {
            line.Front.SetAllWallsDynamic(SectorDynamic.Alpha);
            line.Back.SetAllWallsDynamic(SectorDynamic.Alpha);
            world.RenderBlockmap.LinkDynamicSide(world, line.Front);
            if (line.Front.Sector != line.Back.Sector)
                world.RenderBlockmap.LinkDynamicSide(world, line.Back);
            return;
        }

        if (line.Front.ScrollData != null)
        {
            line.Front.SetAllWallsDynamic(SectorDynamic.Scroll);
            world.RenderBlockmap.LinkDynamicSide(world, line.Front);
        }

        if (line.Back != null && line.Back.ScrollData != null)
        {
            line.Front.SetAllWallsDynamic(SectorDynamic.Scroll);
            world.RenderBlockmap.LinkDynamicSide(world, line.Back);
        }
    }

    private static void CheckFloodFill(WorldBase world, Line line)
    {
        if (line.Back == null)
            return;

        CheckFloodFillSide(world, line.Front, line.Back);
        CheckFloodFillSide(world, line.Back, line.Front);
    }

    private static void CheckFloodFillSide(WorldBase world, Side facingSide, Side otherSide)
    {
        if (facingSide.Lower.TextureHandle == Constants.NoTextureIndex && facingSide.Sector.Floor.Z < otherSide.Sector.Floor.Z)
            facingSide.FloodTextures |= SideTexture.Lower;

        if (facingSide.Upper.TextureHandle == Constants.NoTextureIndex && facingSide.Sector.Ceiling.Z > otherSide.Sector.Ceiling.Z)
            facingSide.FloodTextures |= SideTexture.Upper;
    }

    public static void SetSectorsDynamic(WorldBase world, IEnumerable<Sector> sectors, bool floor, bool ceiling, SectorDynamic sectorDynamic,
        SideTexture lightWalls = AllWallTypes)
    {
        foreach (Sector sector in sectors)
            SetSectorDynamic(world, sector, floor, ceiling, sectorDynamic, lightWalls);
    }

    public static void SetSectorDynamic(WorldBase world, Sector sector, bool floor, bool ceiling, SectorDynamic sectorDynamic,
        SideTexture lightWalls = AllWallTypes)
    {
        if (IsLoading && sectorDynamic == SectorDynamic.Movement)
            return;

        if (sectorDynamic == SectorDynamic.Light)
            return;

        if (floor)
            sector.Floor.Dynamic |= sectorDynamic;
        if (ceiling)
            sector.Ceiling.Dynamic |= sectorDynamic;

        if (sector.BlockmapNodes.Count == 0 && (sectorDynamic == SectorDynamic.Light || sectorDynamic == SectorDynamic.TransferHeights || sectorDynamic == SectorDynamic.Movement || sectorDynamic == SectorDynamic.Scroll))
            world.RenderBlockmap.Link(world, sector);

        if (sectorDynamic == SectorDynamic.Light)
        {
            if (lightWalls == SideTexture.None)
                return;

            SetSectorDynamicLight(sector, lightWalls);
        }
        else if (sectorDynamic == SectorDynamic.Movement)
        {
            SetSectorDynamicMovement(world, sector, floor, ceiling);
        }
        else if (sectorDynamic == SectorDynamic.TransferHeights)
        {
            SetSectorTransferHeights(sector);
        }
    }

    private static void SetSectorTransferHeights(Sector sector)
    {
        for (int i = 0; i < sector.Lines.Count; i++)
        {
            var line = sector.Lines[i];
            if (line.Front.Sector.Id == sector.Id)
                line.Front.SetAllWallsDynamic(SectorDynamic.TransferHeights);
            if (line.Back != null && line.Back.Sector.Id == sector.Id)
                line.Back.SetAllWallsDynamic(SectorDynamic.TransferHeights);
        }
    }

    private static void SetSectorDynamicMovement(WorldBase world, Sector sector, bool floor, bool ceiling)
    {
        for (int i = 0; i < sector.Lines.Count; i++)
        {
            var line = sector.Lines[i];
            SetDynamicMovement(world, sector, line, floor, ceiling);
        }
    }

    private static void SetSectorDynamicLight(Sector sector, SideTexture lightWalls)
    {
        for (int i = 0; i < sector.Lines.Count; i++)
        {
            var line = sector.Lines[i];
            SetDynamicLight(sector, lightWalls, line);
            continue;
        }
    }

    public static void ClearSectorDynamicMovement(IWorld world, SectorPlane plane)
    {
        plane.Dynamic &= ~SectorDynamic.Movement;

        // Floor and ceiling can move independently so don't clear it yet.
        if (plane.Sector.IsMoving || plane.Dynamic.HasFlag(SectorDynamic.TransferHeights))
            return;

        plane.Sector.UnlinkFromWorld(world);

        bool floor = plane.Facing == SectorPlaneFace.Floor;
        bool ceiling = plane.Facing == SectorPlaneFace.Ceiling;

        for (int i = 0; i < plane.Sector.Lines.Count; i++)
            ClearDynamicMovement(plane.Sector.Lines[i], floor, ceiling);
    }

    private static void SetDynamicLight(Sector sector, SideTexture lightWalls, Line line)
    {
        if (line.Front.Sector.Id == sector.Id)
            line.Front.SetWallsDynamic(lightWalls, SectorDynamic.Light);

        if (line.Back != null && line.Back.Sector.Id == sector.Id)
            line.Back.SetWallsDynamic(lightWalls, SectorDynamic.Light);
    }

    private static bool SetDynamicMovement(WorldBase world, Sector sector, Line line, bool floor, bool ceiling)
    {
        bool isFloorSky = world.TextureManager.IsSkyTexture(sector.Floor.TextureHandle);
        bool isCeilingSky = world.TextureManager.IsSkyTexture(sector.Ceiling.TextureHandle);

        if (floor && !ceiling)
        {
            if (line.Back != null)
                line.Back.SetWallsDynamic(AllWallTypes, SectorDynamic.Movement);

            line.Front.SetWallsDynamic(AllWallTypes, SectorDynamic.Movement);
            return true;
        }
        else if (!floor && ceiling)
        {
            if (line.Back != null)
                line.Back.SetWallsDynamic(AllWallTypes, SectorDynamic.Movement);

            line.Front.SetWallsDynamic(AllWallTypes, SectorDynamic.Movement);
            return true;
        }

        return false;
    }

    private static void ClearDynamicMovement(Line line, bool floor, bool ceiling)
    {
        if (line.Front.Sector.IsMoving)
            return;

        if (line.Back != null && line.Back.Sector.IsMoving)
            return;

        if (floor && !ceiling)
        {
            if (line.Back != null && !line.Back.Sector.IsMoving)
                line.Back.ClearWallsDynamic(AllWallTypes, SectorDynamic.Movement);

            line.Front.ClearWallsDynamic(AllWallTypes, SectorDynamic.Movement);
        }
        else if (!floor && ceiling)
        {
            if (line.Back != null)
                line.Back.ClearWallsDynamic(AllWallTypes, SectorDynamic.Movement);

            line.Front.ClearWallsDynamic(AllWallTypes, SectorDynamic.Movement);
        }
    }
}
