using Helion.Util.Configs.Components;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.Specials;
using Helion.World.Special;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helion.Maps.Specials;
using Helion.World.Entities.Definition.States;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Special.Switches;
using Helion.Resources;
using System.Reflection.Metadata.Ecma335;

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

        foreach (var bossDeathSpecial in world.BossDeathSpecials)
        {
            var sectors = world.FindBySectorTag(bossDeathSpecial.SectorTag);
            bool floor = bossDeathSpecial.IsFloorMove();
            bool ceiling = bossDeathSpecial.IsCeilingMove();
            if (!floor && !ceiling)
                continue;

            SetSectorsDynamic(world, sectors, floor, ceiling, SectorDynamic.Movement);
        }

        SetLevelModificationFrames(world);

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
        SectorMovementLookup.Clear();
    }

    private static void SetLevelModificationFrames(WorldBase world)
    {
        var sector = Sector.CreateDefault();
        foreach (var frame in world.ArchiveCollection.EntityFrameTable.Frames)
        {
            if (frame.ActionFunction == EntityActionFunctions.A_KeenDie)
            {
                var sectors = world.FindBySectorTag(666);
                SetSectorsDynamic(world, sectors, false, true, SectorDynamic.Movement);
            }
            else if (frame.ActionFunction == EntityActionFunctions.A_LineEffect)
            {
                SpecialArgs specialArgs = new();
                if (world.Sectors.Count == 0 || !EntityActionFunctions.CreateLineEffectSpecial(frame, out var lineSpecial, out var flags, ref specialArgs))
                    continue;

                Line line = EntityActionFunctions.CreateDummyLine(flags, lineSpecial, specialArgs, sector);
                DetermineStaticSectorLine(world, line);
            }
        }
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

        if (!StaticLights)
        {
            if (sector.TransferFloorLightSector.Id != sector.Id && !sector.TransferFloorLightSector.IsFloorStatic)
                SetSectorDynamic(world, sector, true, false, SectorDynamic.Light, SideTexture.None);

            if (sector.TransferCeilingLightSector.Id != sector.Id && !sector.TransferCeilingLightSector.IsCeilingStatic)
                SetSectorDynamic(world, sector, false, true, SectorDynamic.Light, SideTexture.None);
        }
    }

    private static void DetermineStaticSectorLine(WorldBase world, Line line)
    {
        if (line.Back != null && line.Alpha < 1)
        {
            line.Front.SetAllWallsDynamic(SectorDynamic.Alpha);
            line.Back.SetAllWallsDynamic(SectorDynamic.Alpha);
            return;
        }

        if (line.Front.ScrollData != null)
        {
            line.Front.SetAllWallsDynamic(SectorDynamic.Scroll);
            world.Blockmap.Link(world, line.Front.Sector);
        }

        if (line.Back != null && line.Back.ScrollData != null)
        {
            line.Front.SetAllWallsDynamic(SectorDynamic.Scroll);
            world.Blockmap.Link(world, line.Back.Sector);
        }

        var special = line.Special;
        if (special == LineSpecial.Default)
            return;

        if (special.IsSectorSpecial() && !special.IsSectorStopMove())
        {
            var sectors = world.SpecialManager.GetSectorsFromSpecialLine(line);

            if (special.IsStairBuild())
                SetStairBuildDynamic(world, line, special, sectors);
            else if (special.IsFloorDonut())
                SetFloorDonutDynamic(world, special, sectors);
            if (special.IsFloorMove() && special.IsCeilingMove())
                SetSectorsDynamic(world, sectors, true, true, SectorDynamic.Movement);
            else if (special.IsFloorMove())
                SetSectorsDynamic(world, sectors, true, false, SectorDynamic.Movement);
            else if (special.IsCeilingMove())
                SetSectorsDynamic(world, sectors, false, true, SectorDynamic.Movement);
            else if (!StaticLights && !special.IsTransferLight() && !special.IsSectorFloorTrigger())
                SetSectorsDynamic(world, sectors, true, true, SectorDynamic.Light);
        }
    }

    private static void SetFloorDonutDynamic(WorldBase world, LineSpecial lineSpecial, IEnumerable<Sector> sectors)
    {
        foreach (var sector in sectors)
            SetSectorsDynamic(world, DonutSpecial.GetDonutSectors(sector), lineSpecial.IsFloorMove(), lineSpecial.IsCeilingMove(), SectorDynamic.Movement);
    }

    private static void SetStairBuildDynamic(WorldBase world, Line line, LineSpecial lineSpecial, IEnumerable<Sector> sectors)
    {
        foreach (var sector in sectors)
        {
            ISpecial? special = world.SpecialManager.CreateSingleSectorSpecial(line, lineSpecial, sector);
            if (special == null || special is not StairSpecial stairSpecial)
                continue;

            var stairSectors = stairSpecial.GetBuildSectors();
            SetSectorsDynamic(world, stairSectors, lineSpecial.IsFloorMove(), lineSpecial.IsCeilingMove(), SectorDynamic.Movement);

            // Need to clear any floor movement pointers set from the created special
            foreach (var stairSector in stairSectors)
                stairSector.ClearActiveMoveSpecial();
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

        if (sectorDynamic == SectorDynamic.Scroll || sectorDynamic == SectorDynamic.Light || sectorDynamic == SectorDynamic.TransferHeights)
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
                if (SetDynamicMovement(line, floor, ceiling))
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

    public static void ClearSectorDynamicMovement(SectorPlane plane)
    {
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

    private static bool SetDynamicMovement(Line line, bool floor, bool ceiling)
    {
        if (floor && !ceiling)
        {
            if (line.Back != null)
                line.Back.SetWallsDynamic(MiddleLower, SectorDynamic.Movement);

            line.Front.SetWallsDynamic(MiddleLower, SectorDynamic.Movement);
            return true;
        }
        else if (!floor && ceiling)
        {
            if (line.Back != null)
                line.Back.SetWallsDynamic(MiddleUpper, SectorDynamic.Movement);

            line.Front.SetWallsDynamic(MiddleUpper, SectorDynamic.Movement);
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
