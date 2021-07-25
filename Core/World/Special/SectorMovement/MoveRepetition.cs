namespace Helion.World.Special.SectorMovement
{
    /// <summary>
    /// Defines how sector movement repeats during it's lifetime.
    /// </summary>
    public enum MoveRepetition
    {
        /// <summary>
        /// The move special will destroy when it hits it's destination. The
        /// move special will continue to try to move while being blocked by
        /// an entity. Used with floor raise to, Ceiling lower to specials etc.
        /// </summary>
        None,
        
        /// <summary>
        /// The move special will destroy when it hits it's destination. If
        /// blocked it will return to it's start position. Used with door
        /// close specials, etc.
        /// </summary>
        ReturnOnBlock,
        
        /// <summary>
        /// The move special will destroy when it returns to it start position.
        /// Movement flips will delay. The move special will continually flip
        /// direction while being blocked by an entity. Used by door and lift
        /// specials.
        /// </summary>
        DelayReturn,
        
        /// <summary>
        /// The move special will move back and forth until signaled to stop.
        /// The move special will continue to try to move while being blocked
        /// by an entity. Used with moving floor perpetual specials.
        /// </summary>
        Perpetual,

        /// <summary>
        /// Like Perpetual, but the move special will pause when it hits it's destination.
        /// </summary>
        PerpetualPause,
    }
}