using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.Geometry
{
    /// <summary>
    /// A BSP segment that contains extra line information in addition to a
    /// double-based segment.
    /// </summary>
    public class BspSegment : Seg2D
    {
        /// <summary>
        /// The vertex index starting index in the vertex allocator.
        /// </summary>
        public readonly int StartIndex;

        /// <summary>
        /// The vertex index ending index in the vertex allocator.
        /// </summary>
        public readonly int EndIndex;

        /// <summary>
        /// The ID of the line this is or is a subset of.
        /// </summary>
        public readonly Line? Line;

        /// <summary>
        /// The sector on the front side. This is null if it's a miniseg.
        /// </summary>
        public Sector? FrontSector => Line.Front.Sector;

        /// <summary>
        /// The sector on the back side. This is null if it's a miniseg or if
        /// it's a one sided line.
        /// </summary>
        public Sector? BackSector => Line.Back?.Sector;

        /// <summary>
        /// True if this is a one sided segment, false if not.
        /// </summary>
        /// <remarks>
        /// This does not take into account miniseg status.
        /// </remarks>
        public bool OneSided => Line?.OneSided ?? false;

        /// <summary>
        /// True if this is two sided, false if not.
        /// </summary>
        public bool TwoSided => !IsMiniseg && !OneSided;

        /// <summary>
        /// True if this is a miniseg, false otherwise.
        /// </summary>
        public bool IsMiniseg => Line == null;

        /// <summary>
        /// Creates a new BSP segment from the data provided.
        /// </summary>
        /// <param name="start">The start vertex.</param>
        /// <param name="end">The end vertex.</param>
        /// <param name="startIndex">The index of the start vertex.</param>
        /// <param name="endIndex">The index of the end vertex.</param>
        /// <param name="line">The line (if any, this being null implies it is
        /// a miniseg).</param>
        public BspSegment(Vec2D start, Vec2D end, int startIndex, int endIndex, Line? line = null) : 
            base(start, end)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            Line = line;

            Postcondition(Length() >= 0.00001, "Extremely small split detected");
        }

        /// <summary>
        /// Gets the endpoint enumeration from the vertex index. The index 
        /// provided should be one of the start or end indices.
        /// </summary>
        /// <param name="index">The index to get the endpoint form.</param>
        /// <returns>The start enumeration if the index is equal to the start
        /// index in this segment, otherwise the end enumeration.</returns>
        public Endpoint EndpointFrom(int index) => index == StartIndex ? Endpoint.Start : Endpoint.End;

        /// <summary>
        /// Gets the opposite endpoint enumeration from the vertex index. The
        /// index provided should be one of the start or end indices.
        /// </summary>
        /// <param name="index">The index to get the opposite endpoint form.
        /// </param>
        /// <returns>The end enumeration if the index is equal to the start
        /// index in this segment, otherwise the start enumeration.</returns>
        public Endpoint OppositeEndpoint(int index) => index == StartIndex ? Endpoint.End : Endpoint.Start;

        /// <summary>
        /// Gets the index from the endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to get.</param>
        /// <returns>The index for the endpoint.</returns>
        public int IndexFrom(Endpoint endpoint) => endpoint == Endpoint.Start ? StartIndex : EndIndex;

        /// <summary>
        /// Gets the opposite index from the endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint index opposite to this.</param>
        /// <returns>The index for the opposite endpoint.</returns>
        public int OppositeIndex(Endpoint endpoint) => endpoint == Endpoint.Start ? EndIndex : StartIndex;

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"({Start}) -> ({End}) [line={Line?.Id}, oneSided={OneSided} miniseg={IsMiniseg}]";
        }
    }
}