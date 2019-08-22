namespace Helion.Maps.Special
{
    public class VanillaSectorSpecTranslator
    {
        public static ZSectorSpecialType Translate(VSectorSpecialType type)
        {
            switch (type)
            {
                case VSectorSpecialType.LightBlinkRandom:
                    return ZSectorSpecialType.LightFlickerDoom;

                case VSectorSpecialType.LightBlinkHalfSecond:
                    return ZSectorSpecialType.LightStrobeFastDoom;

                case VSectorSpecialType.LightBlinkOneSecond:
                    return ZSectorSpecialType.LightStrobeSlowDoom;

                case VSectorSpecialType.DamageFloor10_20_AndStrobe:
                    return ZSectorSpecialType.LightStrobeHurtDoom;

                case VSectorSpecialType.DamageFloor5_10:
                    return ZSectorSpecialType.DamageHellslime;

                case VSectorSpecialType.DamageFloor2_5:
                    return ZSectorSpecialType.DamageNukage;

                case VSectorSpecialType.LightStrobeOnePlusSecond:
                    return ZSectorSpecialType.LightGlow;

                case VSectorSpecialType.Secret:
                    return ZSectorSpecialType.Secret;

                case VSectorSpecialType.DoorCloseStay30Seconds:
                    return ZSectorSpecialType.SectorDoorClose30Seconds;

                case VSectorSpecialType.DamageFloor10_20_EndLevel:
                    return ZSectorSpecialType.DamageEnd;

                case VSectorSpecialType.LightStrobeOneSecond:
                    return ZSectorSpecialType.LightStrobeSlowSync;

                case VSectorSpecialType.LightBlinkQuarterSecond:
                    return ZSectorSpecialType.LightStrobeFastSync;

                case VSectorSpecialType.DoorOpenCloseAfter5Minutes:
                    return ZSectorSpecialType.DoorRaiseIn5Minutes;

                case VSectorSpecialType.DamageFloor10_20:
                    return ZSectorSpecialType.DamageSuperHell;

                case VSectorSpecialType.LightFlicker:
                    return ZSectorSpecialType.LightFireFlicker;
            }

            return ZSectorSpecialType.None;
        }
    }
}
