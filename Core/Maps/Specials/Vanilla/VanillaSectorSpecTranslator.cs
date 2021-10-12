using Helion.Maps.Specials.ZDoom;

namespace Helion.Maps.Specials.Vanilla;

public static class VanillaSectorSpecTranslator
{
    public static ZDoomSectorSpecialType Translate(int sectorType, SectorData sectorData)
    {
        if (sectorType == 0)
        {
            sectorData.Clear();
            return ZDoomSectorSpecialType.None;
        }

        VanillaSectorSpecialType type = (VanillaSectorSpecialType)SectorSpecialData.GetType(sectorType, SectorDataType.Boom);
        SectorSpecialData.SetSectorData(sectorType, sectorData, SectorDataType.Boom);

        switch (type)
        {
            case VanillaSectorSpecialType.LightBlinkRandom:
                return ZDoomSectorSpecialType.LightFlickerDoom;
            case VanillaSectorSpecialType.LightBlinkHalfSecond:
                return ZDoomSectorSpecialType.LightStrobeFastDoom;
            case VanillaSectorSpecialType.LightBlinkOneSecond:
                return ZDoomSectorSpecialType.LightStrobeSlowDoom;
            case VanillaSectorSpecialType.DamageFloor10_20_AndStrobe:
                return ZDoomSectorSpecialType.LightStrobeHurtDoom;
            case VanillaSectorSpecialType.DamageFloor5_10:
                return ZDoomSectorSpecialType.DamageHellslime;
            case VanillaSectorSpecialType.DamageFloor2_5:
                return ZDoomSectorSpecialType.DamageNukage;
            case VanillaSectorSpecialType.LightStrobeOnePlusSecond:
                return ZDoomSectorSpecialType.LightGlow;
            case VanillaSectorSpecialType.Secret:
                sectorData.Secret = true;
                return ZDoomSectorSpecialType.Secret;
            case VanillaSectorSpecialType.DoorCloseStay30Seconds:
                return ZDoomSectorSpecialType.SectorDoorClose30Seconds;
            case VanillaSectorSpecialType.DamageFloor10_20_EndLevel:
                return ZDoomSectorSpecialType.DamageEnd;
            case VanillaSectorSpecialType.LightStrobeOneSecond:
                return ZDoomSectorSpecialType.LightStrobeSlowSync;
            case VanillaSectorSpecialType.LightBlinkQuarterSecond:
                return ZDoomSectorSpecialType.LightStrobeFastSync;
            case VanillaSectorSpecialType.DoorOpenCloseAfter5Minutes:
                return ZDoomSectorSpecialType.DoorRaiseIn5Minutes;
            case VanillaSectorSpecialType.DamageFloor10_20:
                return ZDoomSectorSpecialType.DamageSuperHell;
            case VanillaSectorSpecialType.LightFlicker:
                return ZDoomSectorSpecialType.LightFireFlicker;
            default:
                break;
        }

        return ZDoomSectorSpecialType.None;
    }
}
