using Helion.Render.Shared.World.ViewClipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Render.Shared.World.ViewClipping
{
    [TestClass]
    public class ClipSpanTest
    {
        private const uint Start = 123984;
        private const uint End = 384728799;
        
        [TestMethod]
        public void CreateSpan()
        {
            ClipSpan clipSpan = new ClipSpan(Start, End);
            Assert.AreEqual(Start, clipSpan.StartAngle);
            Assert.AreEqual(End, clipSpan.EndAngle);
        }
        
        [TestMethod]
        public void ContainsSingleValue()
        {
            ClipSpan clipSpan = new ClipSpan(Start, End);
            
            Assert.IsTrue(clipSpan.Contains(Start));
            Assert.IsTrue(clipSpan.Contains(End));
            Assert.IsTrue(clipSpan.Contains((Start + End) / 2));
            Assert.IsFalse(clipSpan.Contains(Start - 1));
            Assert.IsFalse(clipSpan.Contains(End + 1));
            Assert.IsFalse(clipSpan.Contains(0));
            Assert.IsFalse(clipSpan.Contains(uint.MaxValue));
        }
        
        [TestMethod]
        public void ContainsRange()
        {
            ClipSpan clipSpan = new ClipSpan(Start, End);
            
            Assert.IsTrue(clipSpan.Contains(Start, End));
            Assert.IsTrue(clipSpan.Contains(Start, Start));
            Assert.IsTrue(clipSpan.Contains(End, End));
            Assert.IsTrue(clipSpan.Contains(Start + 1, End - 1));
            Assert.IsTrue(clipSpan.Contains((Start + End) / 2, (Start + End) / 2));
            Assert.IsFalse(clipSpan.Contains(Start - 1, End));
            Assert.IsFalse(clipSpan.Contains(Start, End + 1));
            Assert.IsFalse(clipSpan.Contains(Start - 1, End + 1));
            Assert.IsFalse(clipSpan.Contains(0, uint.MaxValue));
        }
    }
}