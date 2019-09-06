namespace Helion.Maps.Doom.Components.Types
{
    public enum DoomSectorType
    {
        None = 0,
        BlinkRandom = 1,
        BlinkHalfSecond = 2,
        BlinkFullSecond = 3,
        DamageTwentyAndBlinkHalfSecond = 4,
        DamageTen = 5,
        DamageFive = 7,
        // 6 is unused.
        LightOscillates = 8,
        Secret = 9,
        Door = 10,
        EndLevelTwentyDamage = 11,
        BlinkHalfSecondSynchronized = 12,
        BlinkFullSecondSynchronized = 13,
        DoorOpensAfterFiveMinutes = 14,
        // 15 is unused.
        DamageTwenty = 16,
        BlinkRandomly = 17,
    }
}