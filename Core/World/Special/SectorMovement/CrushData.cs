using Helion.Maps.Specials.ZDoom;
using Helion.Models;

namespace Helion.World.Special.SectorMovement;

public struct CrushData
{
    public readonly ZDoomCrushMode CrushMode;
    public readonly int Damage;
    public readonly double ReturnFactor;

    public static readonly CrushData Default = new(ZDoomCrushMode.DoomWithSlowDown, 10);

    public CrushData(ZDoomCrushMode crushMode, int damage, double returnFactor = 1.0)
    {
        CrushMode = crushMode;
        Damage = damage;
        ReturnFactor = returnFactor;
    }

    public CrushData(CrushDataModel model)
    {
        CrushMode = (ZDoomCrushMode)model.CrushMode;
        Damage = model.Damage;
        ReturnFactor = model.ReturnFactor;
    }

    public CrushDataModel ToCrushDataModel()
    {
        return new CrushDataModel()
        {
            CrushMode = (int)CrushMode,
            Damage = Damage,
            ReturnFactor = ReturnFactor
        };
    }

    public override bool Equals(object? obj)
    {
        if (obj is not CrushData crushData)
            return false;

        return crushData.CrushMode == CrushMode &&
            crushData.Damage == Damage &&
            crushData.ReturnFactor == ReturnFactor;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
