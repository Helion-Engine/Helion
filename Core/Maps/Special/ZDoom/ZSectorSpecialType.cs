namespace Helion.Maps.Special
{
    public enum ZSectorSpecialType
    {
        None,
        LightPhased,
        LightSequenceStart,
        LightSequenceSpecial1,
        LightSequenceSpecial2,
        LightFlickerDoom = 65,
        LightStrobeFastDoom,
        LightStrobeSlowDoom,
        LightStrobeHurtDoom,
        DamageHellslime,
        DamageNukage = 71,
        LightGlow,
        Secret, // ???/
        SectorDoorClose30Seconds = 74,
        DamageEnd,
        LightStrobeSlowSync,
        LightStrobeFastSync,
        DoorRaiseIn5Minutes,
        DamageSuperHell = 80,
        LightFireFlicker,
    }
}
