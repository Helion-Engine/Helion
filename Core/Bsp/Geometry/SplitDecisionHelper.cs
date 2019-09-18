using System.Collections.Generic;
using Helion.Maps;
using Helion.Maps.Components;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.Geometry
{
    public class SplitDecisionHelper
    {
        private readonly IMap m_map;
        private bool m_nodesTooInaccurateToUse;
        
        public SplitDecisionHelper(IMap map)
        {
            m_map = map;
        }

        public BspSegment? FindSplitter(int nodeIndex, List<BspSegment> segments, out INode? node)
        {
            node = null;
            
            if (m_nodesTooInaccurateToUse || m_map.GetNodes().Count == 0)
                return null;

            if (nodeIndex >= m_map.GetNodes().Count)
            {
                Fail("Node index for BSP split decision helper out of range");
                return null;
            }

            node = m_map.GetNodes()[nodeIndex];

            foreach (BspSegment segment in segments)
                if (!segment.IsMiniseg && node.Splitter.Collinear(segment))
                    return segment;

            // TODO: More work needs to be done on this. Unfortunately vanilla
            // has an issue with slime trails due to imprecisions and we might
            // not be able to account for this imprecision safely. More work
            // needs to be done and the splitter should be drawn in the BSP
            // visualizer to see what is going on here.
            //
            // Since we get a bit of a performance boost on maps where it does
            // work, we'll leave this in here for now.
            if (!m_nodesTooInaccurateToUse)
                m_nodesTooInaccurateToUse = true;
            
            return null;
        }
    }
}