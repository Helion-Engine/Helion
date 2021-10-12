namespace Helion.Bsp.States.Miniseg;

/// <summary>
/// Information on whether the miniseg creator was passing through the void
/// (the empty space outside of the map) or the non-void inside of the map.
/// </summary>
public enum VoidStatus
{
    NotStarted,
    InVoid,
    NotInVoid,
}

