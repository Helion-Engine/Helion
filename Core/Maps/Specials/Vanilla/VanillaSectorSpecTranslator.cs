using Helion.Maps.Specials.ZDoom;

namespace Helion.Maps.Specials.Vanilla
{
    public static class VanillaSectorSpecTranslator
    {
        // TODO: Thoughts on making this an extension method for the enum type? Less typing.
        public static ZDoomSectorSpecialType Translate(VanillaSectorSpecialType type)
        {
            return type switch
            {
                VanillaSectorSpecialType.LightBlinkRandom => ZDoomSectorSpecialType.LightFlickerDoom,
                VanillaSectorSpecialType.LightBlinkHalfSecond => ZDoomSectorSpecialType.LightStrobeFastDoom,
                VanillaSectorSpecialType.LightBlinkOneSecond => ZDoomSectorSpecialType.LightStrobeSlowDoom,
                VanillaSectorSpecialType.DamageFloor10_20_AndStrobe => ZDoomSectorSpecialType.LightStrobeHurtDoom,
                VanillaSectorSpecialType.DamageFloor5_10 => ZDoomSectorSpecialType.DamageHellslime,
                VanillaSectorSpecialType.DamageFloor2_5 => ZDoomSectorSpecialType.DamageNukage,
                VanillaSectorSpecialType.LightStrobeOnePlusSecond => ZDoomSectorSpecialType.LightGlow,
                VanillaSectorSpecialType.Secret => ZDoomSectorSpecialType.Secret,
                VanillaSectorSpecialType.DoorCloseStay30Seconds => ZDoomSectorSpecialType.SectorDoorClose30Seconds,
                VanillaSectorSpecialType.DamageFloor10_20_EndLevel => ZDoomSectorSpecialType.DamageEnd,
                VanillaSectorSpecialType.LightStrobeOneSecond => ZDoomSectorSpecialType.LightStrobeSlowSync,
                VanillaSectorSpecialType.LightBlinkQuarterSecond => ZDoomSectorSpecialType.LightStrobeFastSync,
                VanillaSectorSpecialType.DoorOpenCloseAfter5Minutes => ZDoomSectorSpecialType.DoorRaiseIn5Minutes,
                VanillaSectorSpecialType.DamageFloor10_20 => ZDoomSectorSpecialType.DamageSuperHell,
                VanillaSectorSpecialType.LightFlicker => ZDoomSectorSpecialType.LightFireFlicker,
                _ => ZDoomSectorSpecialType.None
            };
        }
    }
}