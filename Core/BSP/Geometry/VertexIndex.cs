using System;

namespace Helion.BSP.Geometry
{
    /// <summary>
    /// The index of a vertex.
    /// </summary>
    public struct VertexIndex
    {
        public readonly int Index;

        public VertexIndex(int index) => Index = index;

        public static bool operator ==(VertexIndex first, VertexIndex second) => first.Index == second.Index;
        public static bool operator !=(VertexIndex first, VertexIndex second) => first.Index != second.Index;

        public override bool Equals(object obj) => obj is VertexIndex index && Index == index.Index;
        public override int GetHashCode() => HashCode.Combine(Index);
        public override string ToString() => Index.ToString();
    }
}
