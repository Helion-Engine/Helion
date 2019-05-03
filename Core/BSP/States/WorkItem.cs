using Helion.BSP.Geometry;
using System.Collections.Generic;

namespace Helion.BSP.States
{
    public class WorkItem
    {
        public string BranchPath;
        public List<BspSegment> Segments;

        public WorkItem(string branchPath, List<BspSegment> segments)
        {
            BranchPath = branchPath;
            Segments = segments;
        }
    }
}
