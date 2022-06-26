using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public enum PlaneTransferType
{
    // Numeric is determined by the destination (adjacent floor equals this floor)
    Numeric,
    // Set by the triggered line
    Trigger
}

public static class TriggerSpecials
{
    public static void PlaneTransferChange(IWorld world, Sector sector, Line? line, SectorPlaneFace planeType, PlaneTransferType type,
        bool transferSpecial = true)
    {
        if (type == PlaneTransferType.Numeric && GetNumericModelChange(world, sector, planeType, sector.GetZ(planeType),
            out int changeTexture, out SectorDamageSpecial? damageSpecial))
        {
            sector.SetTexture(planeType, changeTexture, world.Gametick);
            if (transferSpecial)
                sector.SectorDamageSpecial = damageSpecial?.Copy(sector);
        }
        else if (type == PlaneTransferType.Trigger && line != null)
        {
            sector.SetTexture(planeType, line.Front.Sector.GetTexture(planeType), world.Gametick);
            if (transferSpecial)
                sector.SectorDamageSpecial = line.Front.Sector.SectorDamageSpecial?.Copy(sector);
        }
    }

    public static bool GetNumericModelChange(IWorld world, Sector sector, SectorPlaneFace planeType,
        double destZ, out int changeTexture, out SectorDamageSpecial? damageSpecial)
    {
        changeTexture = planeType == SectorPlaneFace.Floor ? sector.Floor.TextureHandle : sector.Ceiling.TextureHandle;
        damageSpecial = sector.SectorDamageSpecial;
        bool found = false;
        for (int i = 0; i < sector.Lines.Count; i++)
        {
            Line line = sector.Lines[i];
            if (line.Back == null)
                continue;

            Sector opposingSector = line.Front.Sector == sector ? line.Back.Sector : line.Front.Sector;
            if (planeType == SectorPlaneFace.Floor && opposingSector.Floor.Z == destZ)
            {
                changeTexture = opposingSector.Floor.TextureHandle;
                found = true;
            }
            else if (planeType == SectorPlaneFace.Ceiling && opposingSector.Ceiling.Z == destZ)
            {
                changeTexture = opposingSector.Ceiling.TextureHandle;
                found = true;
            }

            if (found)
            {
                damageSpecial = opposingSector.SectorDamageSpecial?.Copy(sector);
                if (damageSpecial == null)
                    damageSpecial = SectorDamageSpecial.CreateNoDamage(world, sector);
                return true;
            }
        }

        return false;
    }
}
