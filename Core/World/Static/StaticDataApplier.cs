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

        foreach (var bossDeathSpecial in world.BossDeathSpecials)
        {
            var sectors = world.FindBySectorTag(bossDeathSpecial.SectorTag);
            bool floor = bossDeathSpecial.IsFloorMove();
            bool ceiling = bossDeathSpecial.IsCeilingMove();
            if (!floor && !ceiling)
                continue;

            SetSectorsDynamic(sectors, floor, ceiling, SectorDynamic.Movement);
        }

        SetLevelModificationFrames(world);

        foreach (var special in world.SpecialManager.GetSpecials())
        {
            if (special is SectorSpecialBase sectorSpecial)
            {
                SetSectorDynamic(sectorSpecial.Sector, true, true, SectorDynamic.Light);
            }
            else if (special is ScrollSpecial scrollSpecial && scrollSpecial.SectorPlane != null)
            {
                bool floor = scrollSpecial.SectorPlane.Facing == SectorPlaneFace.Floor;
                SetSectorDynamic(scrollSpecial.SectorPlane.Sector, floor, !floor, SectorDynamic.Scroll);
            }
        }

        for (int i = 0; i < world.Sectors.Count; i++)
            DetermineStaticSector(world.Sectors[i], world.TextureManager);

        IsLoading = false;
    }

    private static void SetLevelModificationFrames(WorldBase world)
    {
        var sector = Sector.CreateDefault();
        foreach (var frame in world.ArchiveCollection.EntityFrameTable.Frames)
        {
            if (frame.ActionFunction == EntityActionFunctions.A_KeenDie)
            {
                var sectors = world.FindBySectorTag(666);
                SetSectorsDynamic(sectors, false, true, SectorDynamic.Movement);
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

    private static void DetermineStaticSector(Sector sector, TextureManager textureManager)
    {
        var heights = sector.TransferHeights;
        if (heights != null &&
            (heights.ControlSector.Ceiling.Z < sector.Ceiling.Z || heights.ControlSector.Floor.Z > sector.Floor.Z))
        {
            SetSectorDynamic(sector, true, true, SectorDynamic.TransferHeights);
            return;
        }

        // TODO: SectorDynamic.Movement is not correct, I chose that just to make this work.
        bool isFloorSky = textureManager.IsSkyTexture(sector.Floor.TextureHandle);
        bool isCeilSky = textureManager.IsSkyTexture(sector.Ceiling.TextureHandle);
        if (isFloorSky || isCeilSky)
            SetSectorDynamic(sector, isFloorSky, isCeilSky, SectorDynamic.Movement, SideTexture.None);

        if (sector.TransferFloorLightSector.Id != sector.Id && !sector.TransferFloorLightSector.IsFloorStatic)
            SetSectorDynamic(sector, true, false, SectorDynamic.Light, SideTexture.None);

        if (sector.TransferCeilingLightSector.Id != sector.Id && !sector.TransferCeilingLightSector.IsCeilingStatic)
            SetSectorDynamic(sector, false, true, SectorDynamic.Light, SideTexture.None);
    }

    private static void DetermineStaticSectorLine(WorldBase world, Line line)
    {
        if (line.Back != null && line.Alpha < 1)
        {
            line.Front.DynamicWalls = AllWallTypes;
            line.Back.DynamicWalls = AllWallTypes;
            return;
        }

        if (line.Front.ScrollData != null)
            line.Front.DynamicWalls = AllWallTypes;

        if (line.Back != null && line.Back.ScrollData != null)
            line.Front.DynamicWalls = AllWallTypes;

        if (line.Flags.Activations != LineActivations.None && line.Flags.Activations != LineActivations.CrossLine &&
            SwitchManager.IsLineSwitch(world.ArchiveCollection, line))
        {
            line.Front.DynamicWalls = AllWallTypes;
            if (line.Back != null)
                line.Back.DynamicWalls = AllWallTypes;
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
                SetFloorDonutDynamic(special, sectors);
            if (special.IsFloorMove() && special.IsCeilingMove())
                SetSectorsDynamic(sectors, true, true, SectorDynamic.Movement);
            else if (special.IsFloorMove())
                SetSectorsDynamic(sectors, true, false, SectorDynamic.Movement);
            else if (special.IsCeilingMove())
                SetSectorsDynamic(sectors, false, true, SectorDynamic.Movement);
            else if (special.IsSectorFloorTrigger())
                SetSectorsDynamic(sectors, true, false, SectorDynamic.ChangeFloorTexture);
            else if (!special.IsTransferLight())
                SetSectorsDynamic(sectors, true, true, SectorDynamic.Light);
        }
    }

    private static void SetFloorDonutDynamic(LineSpecial lineSpecial, IEnumerable<Sector> sectors)
    {
        foreach (var sector in sectors)
            SetSectorsDynamic(DonutSpecial.GetDonutSectors(sector), lineSpecial.IsFloorMove(), lineSpecial.IsCeilingMove(), SectorDynamic.Movement);
    }

    private static void SetStairBuildDynamic(WorldBase world, Line line, LineSpecial lineSpecial, IEnumerable<Sector> sectors)
    {
        foreach (var sector in sectors)
        {
            ISpecial? special = world.SpecialManager.CreateSingleSectorSpecial(line, lineSpecial, sector);
            if (special == null || special is not StairSpecial stairSpecial)
                continue;

            var stairSectors = stairSpecial.GetBuildSectors();
            SetSectorsDynamic(stairSectors, lineSpecial.IsFloorMove(), lineSpecial.IsCeilingMove(), SectorDynamic.Movement);

            // Need to clear any floor movement pointers set from the created special
            foreach (var stairSector in stairSectors)
                stairSector.ClearActiveMoveSpecial();
        }
    }

    public static void SetSectorsDynamic(IEnumerable<Sector> sectors, bool floor, bool ceiling, SectorDynamic sectorDynamic,
        SideTexture lightWalls = AllWallTypes)
    {
        foreach (Sector sector in sectors)
            SetSectorDynamic(sector, floor, ceiling, sectorDynamic, lightWalls);
    }

    public static void SetSectorDynamic(Sector sector, bool floor, bool ceiling, SectorDynamic sectorDynamic,
        SideTexture lightWalls = AllWallTypes)
    {
        if (IsLoading && sectorDynamic == SectorDynamic.Movement)
            return;

        if (floor)
            sector.Floor.Dynamic |= sectorDynamic;
        if (ceiling)
            sector.Ceiling.Dynamic |= sectorDynamic;

        if (sectorDynamic == SectorDynamic.ChangeFloorTexture)
            return;

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

            SetLineDynamic(line);
        }
    }

    public static void ClearSectorDynamicMovement(SectorPlane plane)
    {
        bool isMovementOnly = plane.Dynamic == SectorDynamic.Movement;
        plane.Dynamic &= ~SectorDynamic.Movement;

        if (!isMovementOnly)
            return;

        bool floor = plane.Facing == SectorPlaneFace.Floor;
        bool ceiling = plane.Facing == SectorPlaneFace.Ceiling;

        foreach (var line in plane.Sector.Lines)
            ClearDynamicMovement(line, floor, ceiling);
    }

    private static void SetLineDynamic(Line line)
    {
        line.Front.DynamicWalls |= AllWallTypes;
        if (line.Back != null)
            line.Back.DynamicWalls |= AllWallTypes;
    }

    private static void SetDynamicLight(Sector sector, SideTexture lightWalls, Line line)
    {
        if (line.Front.Sector.Id == sector.Id)
            line.Front.DynamicWalls |= lightWalls;

        if (line.Back != null && line.Back.Sector.Id == sector.Id)
            line.Back.DynamicWalls |= lightWalls;
    }

    private static bool SetDynamicMovement(Line line, bool floor, bool ceiling)
    {
        if (floor && !ceiling)
        {
            if (line.Back != null)
                line.Back.DynamicWalls |= MiddleLower;

            line.Front.DynamicWalls |= MiddleLower;
            return true;
        }
        else if (!floor && ceiling)
        {
            if (line.Back != null)
                line.Back.DynamicWalls |= MiddleUpper;

            line.Front.DynamicWalls |= MiddleUpper;
            return true;
        }

        return false;
    }

    private static bool ClearDynamicMovement(Line line, bool floor, bool ceiling)
    {
        if (floor && !ceiling)
        {
            if (line.Back != null)
                line.Back.DynamicWalls &= ~MiddleLower;

            line.Front.DynamicWalls &= ~MiddleLower;
            return true;
        }
        else if (!floor && ceiling)
        {
            if (line.Back != null)
                line.Back.DynamicWalls &= ~MiddleUpper;

            line.Front.DynamicWalls &= ~MiddleUpper;
            return true;
        }

        return false;
    }
}
