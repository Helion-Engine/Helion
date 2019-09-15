using Helion.Maps.Components;
using Helion.Util.Geometry.Segments;
using Helion.Util.Geometry.Segments.Enums;
using Helion.Util.Geometry.Vectors;
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
        /// An index which indicates collinearity with any other segment that
        /// has the same value. See <see cref="CollinearTracker"/> for more
        /// information.
        /// </summary>
        public readonly int CollinearIndex;

        /// <summary>
        /// The ID of the line this is or is a subset of.
        /// </summary>
        public readonly IBspUsableLine? Line;

        /// <summary>
        /// True if this is a one sided segment, false if not.
        /// </summary>
        /// <remarks>
        /// This does not take into account miniseg status.
        /// </remarks>
        public bool OneSided => Line != null && Line.OneSided;

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
        /// <param name="collinearIndex">The index for collinearity. See
        /// <see cref="CollinearTracker"/> for more info.</param>
        /// <param name="line">The line (if any, this being null implies it is
        /// a miniseg).</param>
        public BspSegment(Vec2D start, Vec2D end, int startIndex, int endIndex, int collinearIndex, IBspUsableLine? line = null) : 
            base(start, end)
        {
            Precondition(startIndex != endIndex, "BSP segment shouldn't have a start and end index being the same");
            
            StartIndex = startIndex;
            EndIndex = endIndex;
            CollinearIndex = collinearIndex;
            Line = line;

            Postcondition(Length() >= 0.00001, "Extremely small BSP segment detected");
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

        /// <summary>
        /// Checks if the endpoints are shared, meaning that their vertex index
        /// is the same as one in another segment.
        /// </summary>
        /// <remarks>
        /// This does not check based on position. It functions purely off of
        /// the information from a <see cref="VertexAllocator"/> from which
        /// this segment had its vertex index created from.
        /// </remarks>
        /// <param name="segment">The other segment to check against.</param>
        /// <returns>True if one of the endpoints is shared, false if not.
        /// </returns>
        public bool SharesAnyEndpoints(BspSegment segment)
        {
            return StartIndex == segment.StartIndex ||
                   StartIndex == segment.EndIndex ||
                   EndIndex == segment.StartIndex ||
                   EndIndex == segment.EndIndex;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Line is ILine line)
                return $"({Start}) -> ({End}) [line={line?.Id}, oneSided={OneSided} miniseg={IsMiniseg}]";
            return $"({Start}) -> ({End}) [oneSided={OneSided} miniseg={IsMiniseg}]";
        }
    }
}