namespace Helion.BspOld.States.Convex
{
    /// <summary>
    /// An enumeration for the state of the convex checker.
    /// </summary>
    public enum ConvexState
    {
        Loaded,
        Traversing,
        FinishedIsDegenerate,
        FinishedIsConvex,
        FinishedIsSplittable,
    }
}