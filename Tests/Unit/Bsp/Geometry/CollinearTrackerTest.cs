using System.Collections.Generic;
using Helion.BspOld.Geometry;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Bsp.Geometry
{
    [TestClass]
    public class CollinearTrackerTest
    {
        [TestMethod]
        public void DifferentSlopedLinesHaveUniqueIndices()
        {
            CollinearTracker collinearTracker = new CollinearTracker(0.01);

            HashSet<int> indices = new HashSet<int>
            {
                collinearTracker.GetOrCreateIndex(new Vec2D(0, 0), new Vec2D(2, 3)),
                collinearTracker.GetOrCreateIndex(new Vec2D(5, -2), new Vec2D(8, -2)),
                collinearTracker.GetOrCreateIndex(new Vec2D(4, 4), new Vec2D(4, -8)),
                collinearTracker.GetOrCreateIndex(new Vec2D(4, 4), new Vec2D(-5, -21))
            };
            
            Assert.AreEqual(4, indices.Count);
        }
                
        [TestMethod]
        public void VerticalCollinearLinesMatch()
        {
            CollinearTracker collinearTracker = new CollinearTracker(0.01);

            int first = collinearTracker.GetOrCreateIndex(new Vec2D(0, 0), new Vec2D(0, -3));
            int second = collinearTracker.GetOrCreateIndex(new Vec2D(5, -8), new Vec2D(5, 487));
            int third = collinearTracker.GetOrCreateIndex(new Vec2D(0, 10), new Vec2D(0, 50));
            
            Assert.AreNotEqual(first, second);
            Assert.AreEqual(first, third);
        }
        
        [TestMethod]
        public void HorizontalCollinearLinesMatch()
        {
            CollinearTracker collinearTracker = new CollinearTracker(0.01);

            int first = collinearTracker.GetOrCreateIndex(new Vec2D(0, 1), new Vec2D(-3, 1));
            int second = collinearTracker.GetOrCreateIndex(new Vec2D(5, 3), new Vec2D(5, 9));
            int third = collinearTracker.GetOrCreateIndex(new Vec2D(-5, 1), new Vec2D(-22, 1));
            
            Assert.AreNotEqual(first, second);
            Assert.AreEqual(first, third);
        }
        
        [TestMethod]
        public void SlopedCollinearLinesMatch()
        {
            CollinearTracker collinearTracker = new CollinearTracker(0.01);

            int first = collinearTracker.GetOrCreateIndex(new Vec2D(0, 2), new Vec2D(1, 4));
            int second = collinearTracker.GetOrCreateIndex(new Vec2D(100, 202), new Vec2D(200, 402));
            int third = collinearTracker.GetOrCreateIndex(new Vec2D(-5, -8), new Vec2D(-4, -6));
            
            Assert.AreEqual(first, second);
            Assert.AreEqual(first, third);
        }

        [TestMethod]
        public void HasCollinearityWithinEpsilon()
        {
            CollinearTracker collinearTracker = new CollinearTracker(0.01);
            
            int first = collinearTracker.GetOrCreateIndex(new Vec2D(0, 0), new Vec2D(4, 4));
            int second = collinearTracker.GetOrCreateIndex(new Vec2D(1, 1), new Vec2D(2.001, 1.999));
            
            Assert.AreEqual(first, second);
        }
    }
}