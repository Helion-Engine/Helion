namespace Helion.Util.Geometry
{
    /// <summary>
    /// The direction a segment goes in from Start -> End.
    /// </summary>
    public enum SegmentDirection
    {
        Vertical,
        Horizontal,
        PositiveSlope,
        NegativeSlope
    }

    /// <summary>
    /// The side of a segment that some element can be on.
    /// </summary>
    public enum SegmentSide
    {
        Left,
        On,
        Right
    }

    /// <summary>
    /// A simple enumeration for representing endpoints of a segment.
    /// </summary>
    public enum Endpoint
    {
        Start,
        End
    }
}
