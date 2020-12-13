using Helion.Bsp.Repairer;
using Helion.Maps.Components;
using Helion.Util.Geometry.Graphs;
using Helion.Util.Geometry.Segments;
using Helion.Util.Geometry.Segments.Enums;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.Geometry
{
    /// <summary>
    /// A BSP segment that contains extra line information in addition to a
    /// double-based segment.
    /// </summary>
    public class BspSegment : Seg2D, IGraphEdge
    {
        /// <summary>
        /// The constants for no sector existing.
        /// </summary>
        public const int NoSectorId = -1;

        /// <summary>
        /// A constant line ID for a miniseg (as it references no existing 
        /// line).
        /// </summary>
        public const int MinisegLineId = -1;

        /// <summary>
        /// The starting vertex.
        /// </summary>
        public readonly BspVertex StartVertex;
        
        /// <summary>
        /// The starting vertex. This will be identical to the Start coordinate
        /// position.
        /// </summary>
        public readonly BspVertex EndVertex;

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
        /// The vertex index starting index in the vertex allocator.
        /// </summary>
        public int StartIndex => StartVertex.Index;

        /// <summary>
        /// The vertex index ending index in the vertex allocator.
        /// </summary>
        public int EndIndex => EndVertex.Index;

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
        /// Creates a new BSP segment from the data provided. This will also
        /// add itself to the vertices provided as an edge reference.
        /// </summary>
        /// <param name="start">The start vertex.</param>
        /// <param name="end">The end vertex.</param>
        /// <param name="collinearIndex">The index for collinearity. See
        /// <see cref="CollinearTracker"/> for more info.</param>
        /// <param name="line">The line (if any, this being null implies it is
        /// a miniseg).</param>
        public BspSegment(BspVertex start, BspVertex end, int collinearIndex, IBspUsableLine? line = null) : 
            base(start.Position, end.Position)
        {
            Precondition(start != end, "BSP segment shouldn't have a start and end index being the same");

            StartVertex = start;
            EndVertex = end;
            CollinearIndex = collinearIndex;
            Line = line;
            
            start.Edges.Add(this);
            end.Edges.Add(this);

            Postcondition(Length() >= 0.00001, "Extremely small BSP segment detected");
        }

        /// <summary>
        /// Removes itself from the vertices it is currently linked to. This is
        /// to be called when you want to remove this from being considered by
        /// the vertices as an edge that uses it.
        /// </summary>
        /// <remarks>
        /// There are cases where we may not want the map to have the edge in
        /// it anymore (in particular, in the <see cref="MapRepairer"/>).
        /// </remarks>
        public void UnlinkFromVertices()
        {
            StartVertex.Edges.Remove(this);
            EndVertex.Edges.Remove(this);
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
        public BspVertex VertexFrom(Endpoint endpoint) => endpoint == Endpoint.Start ? StartVertex : EndVertex;
        
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

        public IGraphVertex GetStart() => StartVertex;

        public IGraphVertex GetEnd() => EndVertex;
        
        public override string ToString()
        {
            if (Line is ILine line)
                return $"({Start}) -> ({End}) [line={line.Id}, oneSided={OneSided} miniseg={IsMiniseg} collinearIndex={CollinearIndex}]";
            return $"({Start}) -> ({End}) [oneSided={OneSided} miniseg={IsMiniseg} collinearIndex={CollinearIndex}]";
        }
    }
}