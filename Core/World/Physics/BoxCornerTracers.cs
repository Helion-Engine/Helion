using Helion.Geometry.Segments;

namespace Helion.World.Physics;

public struct BoxCornerTracers
{
    public readonly Seg2D First;
    public readonly Seg2D Second;
    public readonly Seg2D Third;

    public BoxCornerTracers(Seg2D first, Seg2D second, Seg2D third)
    {
        First = first;
        Second = second;
        Third = third;
    }
}

