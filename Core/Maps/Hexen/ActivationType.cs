namespace Helion.Maps.Hexen
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
        ProjectileOrHitscanHitsOrCrossesLine,
        PlayerPushesWall,
        ProjectileCrossesLine,
        PlayerUsePassThrough,
        PlayerLineCrossThrough,
    }
}