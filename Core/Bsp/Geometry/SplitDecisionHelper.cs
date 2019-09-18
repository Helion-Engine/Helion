using System.Collections.Generic;
using Helion.Maps;
using Helion.Maps.Components;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.Geometry
{
    public class SplitDecisionHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly IMap m_map;
        private bool m_corrupt;
        
        public SplitDecisionHelper(IMap map)
        {
            m_map = map;
        }

        public BspSegment? FindSplitter(int nodeIndex, List<BspSegment> segments, out INode? node)
        {
            node = null;
            
            if (m_corrupt || m_map.GetNodes().Count == 0)
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

            if (!m_corrupt)
            {
                m_corrupt = true;
                Log.Warn("Nodes built for the map are malformed, will have to discover node splitters the slow way");
            }
            
            return null;
        }
    }
}