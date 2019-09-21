using System.Collections.Generic;
using Helion.Bsp.Geometry;

namespace Helion.Bsp.Repairer
{
    public class MapRepairer
    {
        public static List<BspSegment> Repair(List<BspSegment> segments)
        {
            MapRepairer repairer = new MapRepairer();
            return repairer.PerformRepair(segments);
        }

        private List<BspSegment> PerformRepair(List<BspSegment> segments)
        {
            // TODO: Remove segments that are a point
            // TODO: Remove double lines on top of each other 
            // TODO: Remove overlapping collinear lines (?)
            // TODO: Fix intersecting lines (E1M3/D2M2)
            // TODO: Fix dangling one sided lines (D2M14)
            // TODO: Fix bridge one sided lines (D2M14)
            return segments;
        }
    }
}