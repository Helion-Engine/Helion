namespace Helion.Maps.Special
{
    /// <summary>
    /// Defines how sector movement repeats during it's lifetime.
    /// None - The move special will destroy when it hits it's destination. The move special will continue to try to move while being blocked by an entity.
    /// e.g. Floor raise to, Ceiling lower to specials etc.
    /// RepeatOnce - The move special will destroy when it returns to it start position. The move special will continually flip direction while being blocked by an entity.
    /// e.g. Door and lift specials.
    /// Perpetual - The move special will move back and forth until signaled to stop. The move special will continue to try to move while being blocked by an entity.
    /// e.g. Start moving floor special.
    /// </summary>
    /// <remarks>
    public enum MoveRepitition
    {
        None,
        RepeatOnce,
        Perpetual,
    }
}
