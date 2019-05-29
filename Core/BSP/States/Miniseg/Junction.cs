using Helion.BSP.Geometry;
using Helion.Util.Geometry;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assert;

namespace Helion.BSP.States.Miniseg
{
    /// <summary>
    /// Represents all the wedges of one sided lines that occur at a vertex.
    /// </summary>
    /// <remarks>
    /// A junction is used in determining whether some point is inside or 
    /// outside of the map. Each junction is composed of a series of wedges,
    /// which are two one-sided-segments that share exactly one vertex. Each
    /// vertex can have many one-sided line segments coming out of it, so the
    /// junction is responsible for picking the two segments that have the
    /// closest angle to each other and making a wedge out of that.
    /// </remarks>
    public class Junction
    {
        /// <summary>
        /// All the inbound segments, which means they end on the vertex for 
        /// this junction.
        /// </summary>
        public readonly List<BspSegment> InboundSegments = new List<BspSegment>();

        /// <summary>
        /// All the outbound segments, which means they start on the vertex for 
        /// this junction.
        /// </summary>
        public readonly List<BspSegment> OutboundSegments = new List<BspSegment>();

        /// <summary>
        /// All the compiled wedges from the inbound/outbound segment pairs.
        /// </summary>
        public readonly List<JunctionWedge> Wedges = new List<JunctionWedge>();

        /// <summary>
        /// Calculates a 'score', which is an arbitrary number that tells us
        /// how close the angle is for the wedge provided.
        /// </summary>
        /// <remarks>
        /// For example, if there were 3 segs A, B, and C, where we were 
        /// trying to see whether AB has a smaller wedge than AC, if the 
        /// score of AB is less than that of AC, it has a smaller angle.
        /// </remarks>
        /// <param name="inbound">The inbound segment.</param>
        /// <param name="outbound">The outbound segment.</param>
        /// <returns>A score that is to be used only for ordering reasons 
        /// (where a lower score means it's a tighter angle than a larger 
        /// score).</returns>
        private static double CalculateAngleScore(BspSegment inbound, BspSegment outbound)
        {
            Vec2D endToOriginPoint = inbound.Start - inbound.End;
            Vec2D startToOriginPoint = outbound.End - outbound.Start;

            double dot = startToOriginPoint.Dot(endToOriginPoint);
            double length = startToOriginPoint.Length() * endToOriginPoint.Length();
            double cosTheta = dot / length;

            double score = cosTheta;
            if (inbound.OnRight(outbound.End))
                score = -score;
            else
                score += 2.0;

            return score;
        }

        /// <summary>
        /// Generates all the wedges from the tracked in/outbound segments.
        /// </summary>
        // TODO: Is there a way we can get rid of having to explicitly call
        // this functIon? I don't like 'having to know to call functions' on
        // a class to make it work. Why not generate it lazily as we need it?
        public void GenerateWedges()
        {
            Precondition(Wedges.Count == 0, "Trying to create BSP junction wedges when they already were made");

            // This is a guard against malformed/dangling one-sided lines.
            if (OutboundSegments.Count == 0)
                return;

            // TODO: Since we know we have one or more outbound segs, we can
            // ignore the check at the start. Any way LINQ can help us write
            // this in a lot less lines of code?
            foreach (BspSegment inbound in InboundSegments) 
            {
                BspSegment closestOutbound = OutboundSegments[0];
                double bestScore = CalculateAngleScore(inbound, closestOutbound);

                for (int i = 1; i < OutboundSegments.Count; i++)
                {
                    double score = CalculateAngleScore(inbound, OutboundSegments[i]);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        closestOutbound = OutboundSegments[i];
                    }
                }

                Wedges.Add(new JunctionWedge(inbound, closestOutbound));
            }
        }

        /// <summary>
        /// Adds a wedge, which implies we know that this is a valid segment.
        /// </summary>
        /// <remarks>
        /// This is intended for when a one-sided segment is split by some
        /// splitter, since we know that new split point will create a wedge.
        /// </remarks>
        /// <param name="inbound">The inbound segment.</param>
        /// <param name="outbound">The outbound segment.</param>
        public void AddWedge(BspSegment inbound, BspSegment outbound)
        {
            Wedges.Add(new JunctionWedge(inbound, outbound));
        }

        /// <summary>
        /// Checks if a point is between the angle of any wedges at this
        /// junction.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if it's between the wedge, false if not.</returns>
        public bool BetweenWedge(Vec2D point)
        {
            return Wedges.Where(w => w.Between(point)).Any();
        }

        /// <summary>
        /// Checks if there is some kind of mismatch in segment counts. Intended
        /// for debugging. This being true implies a malformed map.
        /// </summary>
        /// <returns>True if there is a bad count, false if not.</returns>
        public bool HasUnexpectedSegCount() => InboundSegments.Count != OutboundSegments.Count;
    }
}
