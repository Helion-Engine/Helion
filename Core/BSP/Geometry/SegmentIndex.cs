using System;

namespace Helion.Bsp.Geometry
{
    /// <summary>
    /// The index of a segment.
    /// </summary>
    public struct SegmentIndex
    {
        public readonly int Index;

        internal SegmentIndex(int index) => Index = index;

        public static bool operator ==(SegmentIndex first, SegmentIndex second) => first.Index == second.Index;
        public static bool operator !=(SegmentIndex first, SegmentIndex second) => first.Index != second.Index;

        public override bool Equals(object obj) => obj is SegmentIndex index && Index == index.Index;
        public override int GetHashCode() => HashCode.Combine(Index);
    }
}
