using Helion.BSP.Geometry;
using System.Collections.Generic;

namespace Helion.BSP.States
{
    public class BspWorkItem
    {
        public string BranchPath;
        public List<BspSegment> Segments;

        public BspWorkItem(string branchPath, List<BspSegment> segments)
        {
            BranchPath = branchPath;
            Segments = segments;
        }
    }
}
