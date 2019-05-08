using Helion.BSP.Geometry;
using Helion.Util.Geometry;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assert;

namespace Helion.BSP.States.Miniseg
{
    public class Junction
    {
        public readonly List<BspSegment> InboundSegments = new List<BspSegment>();
        public readonly List<BspSegment> OutboundSegments = new List<BspSegment>();
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

        public void AddWedge(BspSegment inbound, BspSegment outbound)
        {
            Wedges.Add(new JunctionWedge(inbound, outbound));
        }

        public bool BetweenWedge(Vec2D point)
        {
            return Wedges.Where(w => w.Between(point)).Any();
        }

        public bool HasUnexpectedSegCount()
        {
            return InboundSegments.Count != OutboundSegments.Count;
        }
    }
}
