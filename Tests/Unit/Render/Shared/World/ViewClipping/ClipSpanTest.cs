using Helion.Render.Shared.World.ViewClipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Render.Shared.World.ViewClipping
{
    [TestClass]
    public class ClipSpanTest
    {
        private const uint start = 123984;
        private const uint end = 384728799;
        
        [TestMethod]
        public void CreateSpan()
        {
            ClipSpan clipSpan = new ClipSpan(start, end);
            Assert.AreEqual(start, clipSpan.StartAngle);
            Assert.AreEqual(end, clipSpan.EndAngle);
        }
        
        [TestMethod]
        public void ContainsSingleValue()
        {
            ClipSpan clipSpan = new ClipSpan(start, end);
            
            Assert.IsTrue(clipSpan.Contains(start));
            Assert.IsTrue(clipSpan.Contains(end));
            Assert.IsTrue(clipSpan.Contains((start + end) / 2));
            Assert.IsFalse(clipSpan.Contains(start - 1));
            Assert.IsFalse(clipSpan.Contains(end + 1));
            Assert.IsFalse(clipSpan.Contains(0));
            Assert.IsFalse(clipSpan.Contains(uint.MaxValue));
        }
        
        [TestMethod]
        public void ContainsRange()
        {
            ClipSpan clipSpan = new ClipSpan(start, end);
            
            Assert.IsTrue(clipSpan.Contains(start, end));
            Assert.IsTrue(clipSpan.Contains(start, start));
            Assert.IsTrue(clipSpan.Contains(end, end));
            Assert.IsTrue(clipSpan.Contains(start + 1, end - 1));
            Assert.IsTrue(clipSpan.Contains((start + end) / 2, (start + end) / 2));
            Assert.IsFalse(clipSpan.Contains(start - 1, end));
            Assert.IsFalse(clipSpan.Contains(start, end + 1));
            Assert.IsFalse(clipSpan.Contains(start - 1, end + 1));
            Assert.IsFalse(clipSpan.Contains(0, uint.MaxValue));
        }
    }
}