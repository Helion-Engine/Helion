using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Bsp.Node;
using Helion.Bsp.States;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Bsp.States
{
    [TestClass]
    public class WorkItemTest
    {
        [TestMethod]
        public void CanConstructWorkItem()
        {
            const string path = "RLRLL";
            BspNode node = new BspNode();
            BspVertex start = new BspVertex(new Vec2D(0, 0), 0);
            BspVertex end = new BspVertex(new Vec2D(1, 1), 1);
            BspSegment segment = new BspSegment(start, end, 0);
            List<BspSegment> segments = new List<BspSegment> { segment };

            WorkItem workItem = new WorkItem(node, segments, path);
            
            Assert.AreSame(node, workItem.Node);
            Assert.AreSame(segments, workItem.Segments);
            Assert.AreEqual(path, workItem.BranchPath);
        }
    }
}