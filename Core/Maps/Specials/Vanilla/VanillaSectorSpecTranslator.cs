using Helion.Maps.Specials.ZDoom;

namespace Helion.Maps.Specials.Vanilla
{
    public static class VanillaSectorSpecTranslator
    {
        private const int SectorTypeMask = 0x1F;
        private const int SectorDamageMask = 0x60;
        private const int SectorDamageShift = 5;
        private const int SecretFlag = 0x80;
        private const int IceFlag = 0x100;
        private const int WindFlag = 0x200;

        public static ZDoomSectorSpecialType Translate(int sectorType, ref SectorData sectorData)
        {
            sectorData.Secret = (sectorType & SecretFlag) != 0;
            sectorData.DamageAmount = 0;
            sectorData.SectorEffect = SectorEffect.None;
            if (sectorType == 0)
                return ZDoomSectorSpecialType.None;

            VanillaSectorSpecialType type = (VanillaSectorSpecialType)(sectorType & SectorTypeMask);
            sectorData.DamageAmount = GetDamageAmount(sectorType);
            sectorData.SectorEffect = GetSectorEffect(sectorType);

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

        private static int GetDamageAmount(int sectorType)
        {
            return ((sectorType & SectorDamageMask) >> SectorDamageShift) switch
            {
                1 => 5,
                2 => 10,
                3 => 20,
                _ => 0,
            };
        }

        private static SectorEffect GetSectorEffect(int sectorType)
        {
            SectorEffect sectorEffect = SectorEffect.None;
            if ((sectorType & IceFlag) != 0)
                sectorEffect |= SectorEffect.Ice;
            if ((sectorType & WindFlag) != 0)
                sectorEffect |= SectorEffect.Wind;

            return sectorEffect;
        }
    }
}