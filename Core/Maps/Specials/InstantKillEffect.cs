using System;

namespace Helion.Maps.Specials
{
    [Flags]
    // Not really flags, but KillMonsters can be used in combination with any of the others.
    public enum InstantKillEffect
    {
        None,
        KillMonsters = 1,
        KillUnprotectedPlayer  = 2,
        KillPlayer = 4,
        KillAllPlayersExit = 8,
        KillAllPlayersSecretExit = 16
    }
}
