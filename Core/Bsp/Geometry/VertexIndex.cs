namespace Helion.Bsp.Geometry
{
    /// <summary>
    /// The index of a vertex.
    /// </summary>
    public struct VertexIndex
    {
        /// <summary>
        /// The index that makes up this vertex.
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// Creates an vertex index.
        /// </summary>
        /// <param name="index">The index of the vertex.</param>
        public VertexIndex(int index)
        {
            Index = index;
        }

        /// <summary>
        /// Checks for equality of both indices.
        /// </summary>
        /// <param name="first">The first index.</param>
        /// <param name="second">The second index.</param>
        /// <returns>True if they are equal, false if not.</returns>
        public static bool operator ==(VertexIndex first, VertexIndex second) => first.Index == second.Index;
        
        /// <summary>
        /// Checks for inequality of both indices.
        /// </summary>
        /// <param name="first">The first index.</param>
        /// <param name="second">The second index.</param>
        /// <returns>True if they are not equal, false if they equal.</returns>
        public static bool operator !=(VertexIndex first, VertexIndex second) => first.Index != second.Index;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is VertexIndex index && Index == index.Index;
        
        /// <inheritdoc/>
        public override int GetHashCode() => Index.GetHashCode();
        
        /// <inheritdoc/>
        public override string ToString() => Index.ToString();
    }
}