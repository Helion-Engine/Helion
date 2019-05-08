using System.Collections.Generic;
using Helion.BSP.Geometry;
using Helion.Util.Geometry;
using NLog;
using static Helion.Util.Assert;

namespace Helion.BSP.States.Miniseg
{
    public class JunctionClassifier
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private Dictionary<VertexIndex, Junction> vertexToJunction = new Dictionary<VertexIndex, Junction>();

        public bool IsDanglingJunction(VertexIndex vertexIndex)
        {
            if (vertexToJunction.TryGetValue(vertexIndex, out Junction junction))
                return junction.OutboundSegments.Count > 0;
            else
            {
                Fail("Should never fail to get a junction we assumed to have added");
                return false;
            }
        }

        public void AddOneSidedSegment(BspSegment segment)
        {
            Precondition(segment.OneSided, "Adding a two sided segment to something for one sided only");

            Junction endJunction;
            if (!vertexToJunction.TryGetValue(segment.EndIndex, out endJunction))
            {
                endJunction = new Junction();
                vertexToJunction.Add(segment.EndIndex, endJunction);
            }

            Precondition(!endJunction.InboundSegments.Contains(segment), "Adding same segment to inbound junction twice");
            endJunction.InboundSegments.Add(segment);

            Junction startJunction;
            if (!vertexToJunction.TryGetValue(segment.StartIndex, out startJunction))
            {
                startJunction = new Junction();
                vertexToJunction.Add(segment.StartIndex, startJunction);
            }

            Precondition(!startJunction.OutboundSegments.Contains(segment), "Adding same segment to outbound junction twice");
            endJunction.OutboundSegments.Add(segment);
        }

        // TODO: Would like to not have this, it requires the user to know it
        // which is bad design unless we have to for optimization reasons.
        public void NotifyDoneAddingOneSidedSegments()
        {
            foreach ((VertexIndex index, Junction junction) in vertexToJunction)
            {
                if (junction.HasUnexpectedSegCount())
                    log.Warn("BSP junction at index {0} has wrong amount of one-sided lines, BSP tree likely to be malformed", index);

                junction.GenerateWedges();
            }
        }

        public void AddSplitJunction(BspSegment inboundSegment, BspSegment outboundSegment)
        {
            VertexIndex middleVertexIndex = inboundSegment.EndIndex;
            Precondition(outboundSegment.StartIndex == middleVertexIndex, "Adding split junction where inbound/outbound segs are not connected");

            // Because we will be calling this in the middle of the BSP 
            // algorithm, we want to dynamically add them as we go. For 
            // that reason we will invoke the junction creator directly.
            Junction junction = vertexToJunction[middleVertexIndex];
            junction.InboundSegments.Add(inboundSegment);
            junction.OutboundSegments.Add(outboundSegment);
            junction.AddWedge(inboundSegment, outboundSegment);
        }

        public bool CheckCrossingVoid(VertexIndex firstIndex, Vec2D secondVertex)
        {
            if (vertexToJunction.TryGetValue(firstIndex, out Junction junction))
                return junction.BetweenWedge(secondVertex);
            return false;
        }
    }
}
