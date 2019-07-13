using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.Geometry
{
    // TODO: This class sort of sucks, should be done better. Its particular
    //       pain point is when we want to find out the side/direction.
    
    /// <summary>
    /// A BSP segment that contains extra line information in addition to a
    /// double-based segment.
    /// </summary>
    public class BspSegment : Seg2D
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
        /// The vertex index starting index in the vertex allocator.
        /// </summary>
        public readonly VertexIndex StartIndex;

        /// <summary>
        /// The vertex index ending index in the vertex allocator.
        /// </summary>
        public readonly VertexIndex EndIndex;

        /// <summary>
        /// The unique segment index for this segment.
        /// </summary>
        public readonly SegmentIndex SegIndex;

        /// <summary>
        /// The ID of the line this is or is a subset of.
        /// </summary>
        public readonly int LineId;

        /// <summary>
        /// The sector ID for the front sector (if it exists). This is always
        /// a valid number unless it is a miniseg.
        /// </summary>
        public readonly int FrontSectorId;

        /// <summary>
        /// The back sector ID, which is not present unless this is a two sided
        /// line.
        /// </summary>
        public readonly int BackSectorId;

        /// <summary>
        /// True if this is a one sided segment, false if not.
        /// </summary>
        /// <remarks>
        /// This does not take into account miniseg status.
        /// </remarks>
        public readonly bool OneSided;

        /// <summary>
        /// True if this is a miniseg, false otherwise.
        /// </summary>
        public bool IsMiniseg => LineId == MinisegLineId;

        /// <summary>
        /// True if this is two sided, false if not.
        /// </summary>
        /// <remarks>
        /// This does not take into account miniseg status.
        /// </remarks>
        public bool TwoSided => !OneSided;

        public BspSegment(Vec2D start, Vec2D end, VertexIndex startIndex, VertexIndex endIndex,
            SegmentIndex segIndex, int frontSectorId, int backSectorId = NoSectorId, int lineId = MinisegLineId) : 
            base(start, end)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            SegIndex = segIndex;
            FrontSectorId = frontSectorId;
            BackSectorId = backSectorId;
            LineId = lineId;
            OneSided = (!IsMiniseg && backSectorId == NoSectorId);

            // TODO: When we remove GLBSP, remove this... it seems to like cuts
            //       that end up being small (we originally were at 0.001).
            Postcondition(Length() > 0.00001, "Extremely small split detected");
        }

        /// <summary>
        /// Gets the endpoint enumeration from the vertex index. The index 
        /// provided should be one of the start or end indices.
        /// </summary>
        /// <param name="index">The index to get the endpoint form.</param>
        /// <returns>The start enumeration if the index is equal to the start
        /// index in this segment, otherwise the end enumeration.</returns>
        public Endpoint EndpointFrom(VertexIndex index) => index == StartIndex ? Endpoint.Start : Endpoint.End;

        /// <summary>
        /// Gets the opposite endpoint enumeration from the vertex index. The
        /// index provided should be one of the start or end indices.
        /// </summary>
        /// <param name="index">The index to get the opposite endpoint form.
        /// </param>
        /// <returns>The end enumeration if the index is equal to the start
        /// index in this segment, otherwise the start enumeration.</returns>
        public Endpoint OppositeEndpoint(VertexIndex index) => index == StartIndex ? Endpoint.End : Endpoint.Start;

        /// <summary>
        /// Gets the index from the endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to get.</param>
        /// <returns>The index for the endpoint.</returns>
        public VertexIndex IndexFrom(Endpoint endpoint) => endpoint == Endpoint.Start ? StartIndex : EndIndex;

        /// <summary>
        /// Gets the opposite index from the endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint index opposite to this.</param>
        /// <returns>The index for the opposite endpoint.</returns>
        public VertexIndex OppositeIndex(Endpoint endpoint) => endpoint == Endpoint.Start ? EndIndex : StartIndex;

        public override string ToString()
        {
            return $"({Start}) -> ({End}) [front={FrontSectorId}, back={BackSectorId}, lineId={LineId}, oneSided={OneSided}]";
        }
    }
}
