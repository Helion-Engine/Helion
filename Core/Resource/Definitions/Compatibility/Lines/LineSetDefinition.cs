namespace Helion.Resource.Definitions.Compatibility.Lines
{
    /// <summary>
    /// Sets properties on a line.
    /// </summary>
    public class LineSetDefinition : ILineDefinition
    {
        public int Id { get; }
        
        /// <summary>
        /// The new vertex to use for the start endpoint if not null.
        /// </summary>
        public int? StartVertexId;
        
        /// <summary>
        /// The new ending to use for the start endpoint if not null.
        /// </summary>
        public int? EndVertexId;
        
        /// <summary>
        /// The new side to use for the front side if not null.
        /// </summary>
        public int? FrontSideId;
        
        /// <summary>
        /// The new side to use for the back side if not null.
        /// </summary>
        /// <remarks>
        /// This should be ignored if <see cref="RemoveBack"/> is true.
        /// </remarks>
        public int? BackSideId;

        /// <summary>
        /// If true, will flip the line.
        /// </summary>
        /// <remarks>
        /// This should not be mixed with start/end vertex changing because the
        /// changes mean you can just do the flip yourself. This is only for a
        /// writer and reader convenience for the compatibility file.
        /// </remarks>
        public bool Flip;
        
        /// <summary>
        /// If true, will remove the back side and make it a one sided line.
        /// </summary>
        /// <remarks>
        /// If true, this will override setting the back side of the line.
        /// </remarks>
        public bool RemoveBack;

        public LineSetDefinition(int id)
        {
            Id = id;
        }
    }
}