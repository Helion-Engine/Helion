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

        private static int GetSectorIdFrom(BspSegment segment, Endpoint originatingEndpoint,
            SectorLine sectorLine, Rotation rotation)
        {
            // Note that for this method, it is possible for a traversal to go
            // in a direction that would traverse the back side of a one-sided
            // line segment and still be a proper traversal.
            //
            // This occurs because the random segment chooser may pick a two
            // sided line segment at the start, and if so then it has to 
            // arbitrarily pick a direction to go.
            //
            // This direction cannot be known whether it is the correct 
            // direction or not (ex: if it picks a vertical line, do we know
            // if we're on the left side of a convex polygon or the right side?
            // We don't without analyzing lines around it).
            //
            // Because of this (and to keep the code simple), it will go in an
            // arbitary direction and reverse the segments later on if needed.
            // Therefore we can assume that if we hit a one sided segment then
            // it's okay to assume we're grabbing the correct side since the
            // user should not be providing a malformed map.
            if (segment.OneSided)
                return sectorLine.FrontSectorId;

            // If we're moving along with the line...
            if (originatingEndpoint == Endpoint.Start)
                return rotation == Rotation.Right ? sectorLine.FrontSectorId : sectorLine.BackSectorId;
            else
                return rotation == Rotation.Right ? sectorLine.BackSectorId : sectorLine.FrontSectorId;

            //if (segment.SameDirection(sectorLine.Delta))
            //    return rotation == Rotation.Right ? sectorLine.FrontSectorId : sectorLine.BackSectorId;
            //else
            //    return rotation == Rotation.Right ? sectorLine.BackSectorId : sectorLine.FrontSectorId;
        }

        private static List<SubsectorEdge> CreateSubsectorEdges(ConvexTraversal convexTraversal, 
            IList<SectorLine> lineToSectors, Rotation rotation)
        {
            List<ConvexTraversalPoint> traversal = convexTraversal.Traversal;
            Precondition(traversal.Count >= 3, "Traversal must yield at least a triangle in size");

            List<SubsectorEdge> subsectorEdges = new List<SubsectorEdge>();

            ConvexTraversalPoint firstTraversal = traversal.First();
            Vec2D startPoint = firstTraversal.ToPoint();
            foreach (ConvexTraversalPoint traversalPoint in traversal)
            {
                BspSegment segment = traversalPoint.Segment;
                Endpoint originatingEndpoint = traversalPoint.Endpoint;
                Vec2D endingPoint = segment.Opposite(traversalPoint.Endpoint);

                if (segment.IsMiniseg)
                    subsectorEdges.Add(new SubsectorEdge(startPoint, endingPoint));
                else
                {
                    Precondition(segment.LineId < lineToSectors.Count, "Segment has bad line ID or line to sectors list is invalid");

                    SectorLine sectorLine = lineToSectors[segment.LineId];
                    int sectorId = GetSectorIdFrom(segment, originatingEndpoint, sectorLine, rotation);
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
                    Precondition(edge.SectorId == lastCorrectSector, "Subsector references multiple sectors");
            }

            Precondition(lastCorrectSector != NoSectorId, "Unable to find a sector for the subsector (entire sector is minisegs?)");
        }

        public static IList<SubsectorEdge> FromClockwiseTraversal(ConvexTraversal convexTraversal, 
            IList<SectorLine> lineToSectors, Rotation rotation)
        {
            List<SubsectorEdge> edges = CreateSubsectorEdges(convexTraversal, lineToSectors, rotation);
            if (rotation != Rotation.Left)
                ReverseEdgesMutate(edges);

            AssertValidSubsectorEdges(edges);
            return edges;
        }
    }
}
