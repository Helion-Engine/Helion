using System;

namespace Helion.World.Entities.Players;

public struct PlayerStats
{
    public int DeathCount;
    public int MonsterKillCount;
    public int FriendlyKillCount;
    public int SecretCount;
    public int ItemCount;

    public override readonly bool Equals(object? obj)
    {
        return obj is PlayerStats stats &&
               DeathCount == stats.DeathCount &&
               MonsterKillCount == stats.MonsterKillCount &&
               FriendlyKillCount == stats.FriendlyKillCount &&
               SecretCount == stats.SecretCount &&
               ItemCount == stats.ItemCount;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(DeathCount, MonsterKillCount, FriendlyKillCount, SecretCount, ItemCount);
    }
    public static bool operator ==(PlayerStats left, PlayerStats right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PlayerStats left, PlayerStats right)
    {
        return !(left == right);
    }
}
