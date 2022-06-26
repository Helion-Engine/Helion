namespace Helion.World.Stats;

public class LevelStats
{
    public int TotalMonsters { get; set; }
    public int TotalItems { get; set; }
    public int TotalSecrets { get; set; }

    public int KillCount { get; set; }
    public int ItemCount { get; set; }
    public int SecretCount { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is not LevelStats stats)
            return false;

        return stats.TotalMonsters == TotalMonsters && stats.TotalItems == TotalItems && stats.TotalSecrets == TotalSecrets &&
            stats.KillCount == KillCount && stats.ItemCount == ItemCount && stats.SecretCount == SecretCount;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
