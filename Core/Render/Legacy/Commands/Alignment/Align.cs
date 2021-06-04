namespace Helion.Render.Legacy.Commands.Alignment
{
    /// <summary>
    /// An alignment position to anchor from.
    /// </summary>
    /// <remarks>
    /// The first word is the Y position, second word is the X position.
    ///
    ///                     (TopMiddle)
    /// (TopLeft)    o-----------o-----------o (TopRight)
    ///              |                       |
    ///              |        (Center)       |
    /// (MiddleLeft) o           o           o (MiddleRight)
    ///              |                       |
    ///              |                       |
    /// (BottomLeft) o-----------o-----------o (BottomRight)
    ///                   (BottomMiddle)
    /// </remarks>
    public enum Align
    {
        TopLeft,
        TopMiddle,
        TopRight,
        MiddleLeft,
        Center,
        MiddleRight,
        BottomLeft,
        BottomMiddle,
        BottomRight
    }
}
