namespace Helion.Render.Legacy.Shared.World;

/// <summary>
/// Contains the drawing information for what is visible vertically in some
/// two sided middle opening so we know how to draw a middle texture.
/// </summary>
public struct MiddleDrawSpan
{
    /// <summary>
    /// The top Z location of the middle texture. This is where it would be
    /// drawn if there were enough space. This is for UV calculations.
    /// </summary>
    public readonly double TopZ;

    /// <summary>
    /// The bottom Z location of the middle texture. This is where it would
    /// be drawn if there were enough space. This is for UV calculations.
    /// </summary>
    public readonly double BottomZ;

    /// <summary>
    /// The top Z which is visible and safe to draw from. This should be
    /// used when triangulating.
    /// </summary>
    public readonly double VisibleTopZ;

    /// <summary>
    /// The bottom Z which is visible and safe to draw from. This should be
    /// used when triangulating.
    /// </summary>
    public readonly double VisibleBottomZ;

    public MiddleDrawSpan(double bottomZ, double topZ, double visibleBottomZ, double visibleTopZ)
    {
        BottomZ = bottomZ;
        TopZ = topZ;
        VisibleBottomZ = visibleBottomZ;
        VisibleTopZ = visibleTopZ;
    }

    /// <summary>
    /// Checks if the span is not visible and we can't render anything.
    /// </summary>
    /// <returns>True if we cannot see it and should exit early instead of
    /// trying to render it, false if we can see something and it needs to
    /// be rendered.</returns>
    public bool NotVisible() => VisibleTopZ <= VisibleBottomZ;
}

