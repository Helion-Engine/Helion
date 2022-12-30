namespace Helion.Render.OpenGL.Shared.World;

/// <summary>
/// Contains the drawing information for what is visible vertically in some
/// two sided middle opening so we know how to draw a middle texture.
/// </summary>
public struct MiddleDrawSpan
{
    public readonly double TopZ;
    public readonly double BottomZ;
    public readonly double VisibleTopZ;
    public readonly double VisibleBottomZ;

    public readonly double PrevTopZ;
    public readonly double PrevBottomZ;
    public readonly double PrevVisibleTopZ;
    public readonly double PrevVisibleBottomZ;

    public MiddleDrawSpan(double bottomZ, double topZ, double visibleBottomZ, double visibleTopZ,
        double prevBottomZ, double prevTopZ, double prevVisibleBottomZ, double prevVisibleTopZ)
    {
        BottomZ = bottomZ;
        TopZ = topZ;
        VisibleBottomZ = visibleBottomZ;
        VisibleTopZ = visibleTopZ;

        PrevBottomZ = prevBottomZ;
        PrevTopZ = prevTopZ;
        PrevVisibleBottomZ = prevVisibleBottomZ;
        PrevVisibleTopZ = prevVisibleTopZ;
    }

    /// <summary>
    /// Checks if the span is not visible and we can't render anything.
    /// </summary>
    /// <returns>True if we cannot see it and should exit early instead of
    /// trying to render it, false if we can see something and it needs to
    /// be rendered.</returns>
    public bool NotVisible() => VisibleTopZ <= VisibleBottomZ;
}
