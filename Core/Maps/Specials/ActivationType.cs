namespace Helion.Maps.Specials
{
    /// <summary>
    /// Describes how a special is activated.
    /// </summary>
    public enum ActivationType
    {
        None = -1,
        PlayerLineCross,
        PlayerUse,
        MonsterLineCross,
        PlayerOrMonsterLineCross,
        ProjectileHitsWall,
        PlayerPushesWall,
        ProjectileCrossesLine,
        PlayerUsePassThrough,
        ProjectileHitsOrCrossesLine,
        LevelStart,
    }
}