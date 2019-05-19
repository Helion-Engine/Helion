using Helion.BSP.Geometry;
using Helion.BSP.States.Convex;
using Helion.Util.Geometry;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Helion.Util.Assert;

namespace Helion.BSP.Node
{
    public class SubsectorEdge : Seg2DBase
    {
        public const int NoSectorId = -1;

        public readonly int LineId;
        public readonly int? SectorId;

        public bool IsMiniseg => LineId == BspSegment.MinisegLineId;

        public SubsectorEdge(Vec2D start, Vec2D end) : this(start, end, BspSegment.MinisegLineId, NoSectorId)
        {
        }

        public SubsectorEdge(Vec2D start, Vec2D end, int lineId, int sectorId) : base(start, end)
        {
            LineId = lineId;
            if (sectorId != NoSectorId)
                SectorId = sectorId;
        }

        private static Rotation CalculateRotation(ConvexTraversal convexTraversal)
        {
            List<ConvexTraversalPoint> traversal = convexTraversal.Traversal;
            ConvexTraversalPoint firstTraversal = traversal.First();

            Vec2D first = firstTraversal.Segment[firstTraversal.Endpoint];
            Vec2D second = firstTraversal.Segment.Opposite(firstTraversal.Endpoint);

            // We're essentially doing a sliding window of 3 vertices here, and keep
            // checking each corner in the convex polygon to see which way it rotates.
            for (int i = 1; i < traversal.Count; i++)
            {
                ConvexTraversalPoint traversalPoint = traversal[i];

                // Because the endpoint is referring to the starting point for each
                // segment, we need to get the ending point since the start point
                // is already set in `second`, and `first` is the one before that.
                Vec2D third = traversalPoint.Segment.Opposite(traversalPoint.Endpoint);

                Rotation rotation = Seg2D.Rotation(first, second, third);
                if (rotation != Rotation.On)
                    return rotation;

                first = second;
                second = third;
            }

            Fail("Unable to find rotation for convex traversal");
            return Rotation.On;
        }

        private static int GetSectorIdFrom(BspSegment segment, SectorLine sectorLine, Rotation rotation) {
            if (segment.SameDirection(sectorLine.Delta))
            {
                Precondition(!segment.OneSided || rotation != Rotation.Left, "Trying to get the back sector ID of a one sided line");
                return rotation == Rotation.Right ? sectorLine.FrontSectorId : sectorLine.BackSectorId;
            }
            else
            {
                Precondition(!segment.OneSided || rotation != Rotation.Right, "Trying to get the back sector ID of a one sided line");
                return rotation == Rotation.Right ? sectorLine.BackSectorId : sectorLine.FrontSectorId;
            }
        }

        private static List<SubsectorEdge> CreateSubsectorEdges(ConvexTraversal convexTraversal, IList<SectorLine> lineToSectors, Rotation rotation)
        {
            List<ConvexTraversalPoint> traversal = convexTraversal.Traversal;
            Precondition(traversal.Count >= 3, "Traversal must yield at least a triangle in size");

            List<SubsectorEdge> subsectorEdges = new List<SubsectorEdge>();

            Vec2D startPoint = traversal.First().ToPoint();
            foreach (ConvexTraversalPoint traversalPoint in traversal)
            {
                BspSegment segment = traversalPoint.Segment;
                Vec2D endingPoint = segment.Opposite(traversalPoint.Endpoint);

                if (segment.IsMiniseg)
                    subsectorEdges.Add(new SubsectorEdge(startPoint, endingPoint));
                else
                {
                    Precondition(segment.LineId < lineToSectors.Count, "Segment has bad line ID or line to sectors list is invalid");
                    SectorLine sectorLine = lineToSectors[segment.LineId];
                    int sectorId = GetSectorIdFrom(segment, sectorLine, rotation);
                    subsectorEdges.Add(new SubsectorEdge(startPoint, endingPoint, segment.LineId, sectorId));
                }

                startPoint = endingPoint;
            }

            Postcondition(subsectorEdges.Count == traversal.Count, "Added too many subsector edges in traversal");
            return subsectorEdges;
        }

        /// <summary>
        /// Reverses the edges of the list provided. This will also mutate the
        /// list so it will have all new reversed edges.
        /// </summary>
        /// <param name="edges">The edges to reverse.</param>
        private static void ReverseEdgesMutate(List<SubsectorEdge> edges)
        {
            List<SubsectorEdge> reversedEdges = new List<SubsectorEdge>();

            edges.Reverse();
            edges.ForEach(edge =>
            {
                int sectorId = edge.SectorId ?? NoSectorId;
                reversedEdges.Add(new SubsectorEdge(edge.End, edge.Start, edge.LineId, sectorId));
            });

            edges.Clear();
            edges.AddRange(reversedEdges);
        }

        [Conditional("DEBUG")]
        private static void AssertValidSubsectorEdges(List<SubsectorEdge> edges)
        {
            Precondition(edges.Count >= 3, "Not enough edges");

            int lastCorrectSector = NoSectorId;

            // According to https://stackoverflow.com/questions/204505/preserving-order-with-linq
            // the order is preserved for a Where() invocation, so we do not 
            // need to worry about order being scrambled.
            foreach (SubsectorEdge edge in edges.Where(e => e.SectorId.HasValue))
            {
                if (lastCorrectSector == NoSectorId)
                    lastCorrectSector = edge.SectorId ?? NoSectorId;
                else
                    Precondition(edge.SectorId != lastCorrectSector, "Subsector references multiple sectors");
            }

            Precondition(lastCorrectSector != NoSectorId, "Unable to find a sector for the subsector");
        }

        public static IList<SubsectorEdge> FromClockwiseConvexTraversal(ConvexTraversal convexTraversal, IList<SectorLine> lineToSectors)
        {
            Rotation rotation = CalculateRotation(convexTraversal);
            List<SubsectorEdge> edges = CreateSubsectorEdges(convexTraversal, lineToSectors, rotation);
            if (rotation != Rotation.Left)
                ReverseEdgesMutate(edges);

            AssertValidSubsectorEdges(edges);
            return edges;
        }
    }
}
