namespace Helion.Maps.Geometry.Lines
{
    /// <summary>
    /// Describes how a special is activated.
    /// </summary>
    public enum SpecialActivationType
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
        LevelStart
    }
}
