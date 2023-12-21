using Helion.Maps.Specials;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;

namespace Helion.World.Special.Specials;

public enum PlaneTransferType
{
    // Numeric is determined by the destination (adjacent floor equals this floor)
    Numeric,
    // Set by the triggered line
    Trigger
}

public record struct TriggerChanges(int? Texture, SectorDamageSpecial? DamageSpecial, SectorEffect? SectorEffect, InstantKillEffect? KillEffect)
{
    public TriggerChanges(Line line, SectorPlaneFace planeType) :
        this(line.Front.Sector.GetTexture(planeType),
            line.Front.Sector.SectorDamageSpecial,
            line.Front.Sector.SectorEffect,
            line.Front.Sector.KillEffect)
    { }

    public TriggerChanges() : this(null, null, null, null) { }
}

public static class TriggerSpecials
{
    public static void PlaneTransferChange(IWorld world, Sector sector, Line? line, SectorPlaneFace planeType, PlaneTransferType type,
        bool transferSpecial = true)
    {
        if (type == PlaneTransferType.Numeric && GetNumericModelChange(world, sector, planeType, sector.GetZ(planeType),
            SectorDest.None, out var changes))
        {
            sector.ApplyTriggerChanges(world, changes, planeType, transferSpecial);
        }
        else if (type == PlaneTransferType.Trigger && line != null)
        {
            sector.ApplyTriggerChanges(world, new TriggerChanges(line, planeType), planeType, transferSpecial);
        }
    }

    public static bool GetNumericModelChange(IWorld world, Sector sector, SectorPlaneFace planeType, 
        double destZ, SectorDest sectorDest, out TriggerChanges changes)
    {
        SectorPlaneFace destPlaneType = planeType;
        if (planeType == SectorPlaneFace.Floor)
        {
            if (sectorDest == SectorDest.LowestAdjacentCeiling || sectorDest == SectorDest.HighestAdjacentCeiling || sectorDest == SectorDest.Ceiling)
                destPlaneType = SectorPlaneFace.Ceiling;
        }
        else
        {
            if (sectorDest == SectorDest.LowestAdjacentFloor || sectorDest == SectorDest.HighestAdjacentFloor || sectorDest == SectorDest.Floor)
                destPlaneType = SectorPlaneFace.Floor;
        }
            
        changes = new();
        changes.Texture = planeType == SectorPlaneFace.Floor ? sector.Floor.TextureHandle : sector.Ceiling.TextureHandle;
        changes.DamageSpecial = sector.SectorDamageSpecial;
        changes.SectorEffect = sector.SectorEffect;
        changes.KillEffect = sector.KillEffect;

        for (int i = 0; i < sector.Lines.Count; i++)
        {
            Line line = sector.Lines[i];
            if (line.Back == null)
                continue;

            Sector opposingSector = line.Front.Sector == sector ? line.Back.Sector : line.Front.Sector;
            if (opposingSector.GetZ(destPlaneType) != destZ)
                continue;
            
            changes.Texture = planeType == SectorPlaneFace.Floor ? opposingSector.Floor.TextureHandle : opposingSector.Ceiling.TextureHandle;
            changes.SectorEffect = opposingSector.SectorEffect;
            changes.KillEffect = opposingSector.KillEffect;
            changes.DamageSpecial = opposingSector.SectorDamageSpecial?.Copy(sector);
            if (changes.DamageSpecial == null)
                changes.DamageSpecial = SectorDamageSpecial.CreateNoDamage(world, sector);
            return true; 
        }

        return false;
    }
}
