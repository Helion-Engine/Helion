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

namespace Helion.World.Static;

public class StaticDataApplier
{
    const SideTexture AllWallTypes = SideTexture.Upper | SideTexture.Middle | SideTexture.Lower;
    const SideTexture MiddleLower = SideTexture.Middle | SideTexture.Lower;
    const SideTexture MiddleUpper = SideTexture.Middle | SideTexture.Upper;

    private static bool IsLoading;
    private static bool StaticLights;

    private static readonly HashSet<int> SectorMovementLookup = new();

    public static void DetermineStaticData(WorldBase world)
    {
        IsLoading = true;
        StaticLights = world.Config.Render.StaticLights;
        for (int i = 0; i < world.Lines.Count; i++)
            DetermineStaticSectorLine(world, world.Lines[i]);

        foreach (var special in world.SpecialManager.GetSpecials())
        {
            if (special is SectorSpecialBase sectorSpecial)
            {
                SetSectorDynamic(world, sectorSpecial.Sector, true, true, SectorDynamic.Light);
            }
            //else if (special is ScrollSpecial scrollSpecial && scrollSpecial.SectorPlane != null)
            //{
            //    bool floor = scrollSpecial.SectorPlane.Facing == SectorPlaneFace.Floor;
            //    SetSectorDynamic(world, scrollSpecial.SectorPlane.Sector, floor, !floor, SectorDynamic.Scroll);
            //}
        }

        for (int i = 0; i < world.Sectors.Count; i++)
            DetermineStaticSector(world, world.Sectors[i], world.TextureManager);

        IsLoading = false;
        SectorMovementLookup.Clear();
    }

    private static void DetermineStaticSector(WorldBase world, Sector sector, TextureManager textureManager)
    {
        var heights = sector.TransferHeights;
        if (heights != null &&
            (SectorMovementLookup.Contains(heights.ControlSector.Id) || heights.ControlSector.Ceiling.Z < sector.Ceiling.Z || heights.ControlSector.Floor.Z > sector.Floor.Z))
        {
            bool save = IsLoading;
            IsLoading = false;
            SetSectorDynamic(world, sector, true, true, SectorDynamic.TransferHeights);
            if (SectorMovementLookup.Contains(heights.ControlSector.Id))
                SetSectorDynamic(world, sector, true, true, SectorDynamic.Movement);
            IsLoading = save;
            return;
        }

        var transferFloor = sector.TransferFloorLightSector;
        var transferCeiling = sector.TransferCeilingLightSector;

        // Transfer lights can affect many sectors. Even with StaticLights = true, handle these dynamically for now.
        if (transferFloor.Id != sector.Id && (!transferFloor.IsFloorStatic || transferFloor.DataChanges.HasFlag(SectorDataTypes.Light)))
            SetSectorDynamic(world, sector, true, false, SectorDynamic.Light, SideTexture.None);

        if (transferCeiling.Id != sector.Id && (!transferCeiling.IsFloorStatic || transferCeiling.DataChanges.HasFlag(SectorDataTypes.Light)))
            SetSectorDynamic(world, sector, false, true, SectorDynamic.Light, SideTexture.None);
    }

    private static void DetermineStaticSectorLine(WorldBase world, Line line)
    {
        // Hack for now until we have a better solution.
        line.MarkSeenOnAutomap();

        if (line.Back != null && line.Alpha < 1)
        {
            line.Front.SetAllWallsDynamic(SectorDynamic.Alpha);
            line.Back.SetAllWallsDynamic(SectorDynamic.Alpha);
            world.Blockmap.Link(world, line.Front.Sector);
            if (line.Front.Sector != line.Back.Sector)
                world.Blockmap.Link(world, line.Back.Sector);
            return;
        }

        if (line.Front.ScrollData != null)
        {
            line.Front.SetAllWallsDynamic(SectorDynamic.Scroll);
            world.Blockmap.LinkScrolling(world, line.Front.Sector);
        }

        if (line.Back != null && line.Back.ScrollData != null)
        {
            line.Front.SetAllWallsDynamic(SectorDynamic.Scroll);
            world.Blockmap.LinkScrolling(world, line.Back.Sector);
        }

        var special = line.Special;
        if (special == LineSpecial.Default)
            return;

        if (special.IsSectorSpecial() && !special.IsSectorStopMove())
        {
            var sectors = world.SpecialManager.GetSectorsFromSpecialLine(line);
            if (!StaticLights && !special.IsTransferLight() && !special.IsSectorFloorTrigger())
                SetSectorsDynamic(world, sectors, true, true, SectorDynamic.Light);
        }
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
        {
            //SectorMovementLookup.Add(sector.Id);
            return;
        }

        if (StaticLights && sectorDynamic == SectorDynamic.Light)
            return;

        if (floor)
            sector.Floor.Dynamic |= sectorDynamic;
        if (ceiling)
            sector.Ceiling.Dynamic |= sectorDynamic;

        if (sectorDynamic == SectorDynamic.Scroll)
            world.Blockmap.LinkScrolling(world, sector);
        else if (sectorDynamic == SectorDynamic.Light || sectorDynamic == SectorDynamic.TransferHeights || sectorDynamic == SectorDynamic.Movement)
            world.Blockmap.Link(world, sector);

        foreach (var line in sector.Lines)
        {
            if (sectorDynamic == SectorDynamic.Light)
            {
                if (lightWalls == SideTexture.None)
                    continue;

                SetDynamicLight(sector, lightWalls, line);
                continue;
            }
            else if (sectorDynamic == SectorDynamic.Movement)
            {
                if (SetDynamicMovement(world, sector, line, floor, ceiling))
                    continue;
            }
            else if (sectorDynamic == SectorDynamic.TransferHeights)
            {
                if (line.Front.Sector.Id == sector.Id)
                    line.Front.SetAllWallsDynamic(sectorDynamic);
                if (line.Back != null && line.Back.Sector.Id == sector.Id)
                    line.Back.SetAllWallsDynamic(sectorDynamic);
            }
        }
    }

    public static void ClearSectorDynamicMovement(IWorld world, SectorPlane plane)
    {
        plane.Sector.UnlinkFromWorld(world);
        plane.Dynamic &= ~SectorDynamic.Movement;

        bool floor = plane.Facing == SectorPlaneFace.Floor;
        bool ceiling = plane.Facing == SectorPlaneFace.Ceiling;

        foreach (var line in plane.Sector.Lines)
            ClearDynamicMovement(line, floor, ceiling);
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
            SideTexture types = isFloorSky ? AllWallTypes : MiddleLower;
            if (line.Back != null)
                line.Back.SetWallsDynamic(types, SectorDynamic.Movement);

            line.Front.SetWallsDynamic(types, SectorDynamic.Movement);
            return true;
        }
        else if (!floor && ceiling)
        {
            SideTexture types = isCeilingSky ? AllWallTypes : MiddleUpper;
            if (line.Back != null)
                line.Back.SetWallsDynamic(types, SectorDynamic.Movement);

            line.Front.SetWallsDynamic(types, SectorDynamic.Movement);
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
                line.Back.ClearWallsDynamic(MiddleLower, SectorDynamic.Movement);

            line.Front.ClearWallsDynamic(MiddleLower, SectorDynamic.Movement);
        }
        else if (!floor && ceiling)
        {
            if (line.Back != null)
                line.Back.ClearWallsDynamic(MiddleUpper, SectorDynamic.Movement);

            line.Front.ClearWallsDynamic(MiddleUpper, SectorDynamic.Movement);
        }
    }
}
