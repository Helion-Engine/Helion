using Helion.BSP.Geometry;
using System.Collections.Generic;

namespace Helion.BSP.States
{
    public class BspWorkItem
    {
        public const string RootWorkPath = "";

        public IList<BspSegment> Segments;
        public string BranchPath;

        public BspWorkItem(IList<BspSegment> segments, string branchPath = RootWorkPath)
        {
            Segments = segments;
            BranchPath = branchPath;
        }
    }
}
