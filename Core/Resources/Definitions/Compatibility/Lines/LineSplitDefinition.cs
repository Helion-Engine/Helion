namespace Helion.Resources.Definitions.Compatibility.Lines
{
    /// <summary>
    /// Splits a line at some vertex that is intersecting the line but somehow
    /// was not set to be a splitter for the line (or is a vertex we added).
    /// The line that was split will also be removed for BSP reasons.
    /// </summary>
    /// <remarks>
    /// A great example of this would be D2M2.
    /// </remarks>
    public class LineSplitDefinition : ILineDefinition
    {
        public int Id { get; }
        
        /// <summary>
        /// The ID of the starting line, which is defined as the line from the
        /// start vertex to the split point.
        /// </summary>
        public readonly int StartId;
        
        /// <summary>
        /// The ID of the ending line, which is defined as the line from the
        /// split point to the end vertex.
        /// </summary>
        public readonly int EndId;
        
        /// <summary>
        /// The ID of the vertex to split.
        /// </summary>
        public readonly int SplitVertexId;
        
        public LineSplitDefinition(int id, int startId, int endId, int splitVertexId)
        {
            Id = id;
            StartId = startId;
            EndId = endId;
            SplitVertexId = splitVertexId;
        }
    }
}