using Helion.Util.Geometry.Segments;

namespace Helion.Worlds.Physics
{
    public struct BoxCornerTracers
    {
        public readonly Seg2DBase First;
        public readonly Seg2DBase Second;
        public readonly Seg2DBase Third;

        public BoxCornerTracers(Seg2DBase first, Seg2DBase second, Seg2DBase third)
        {
            First = first;
            Second = second;
            Third = third;
        }
    }
}