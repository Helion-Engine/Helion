namespace Helion.MapsNew.Specials
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
        ProjectileHitsWall,
        PlayerPushesWall,
        ProjectileCrossesLine,
        PlayerUsePassThrough,
        ProjectileHitsOrCrossesLine,
        LevelStart,
    }
}