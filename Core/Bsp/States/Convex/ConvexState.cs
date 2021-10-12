namespace Helion.Bsp.States.Convex;

/// <summary>
/// An enumeration for the state of the convex checker.
/// </summary>
public enum ConvexState
{
    None,
    Loaded,
    Traversing,
    FinishedIsDegenerate,
    FinishedIsConvex,
    FinishedIsSplittable,
}

