using Helion.Util.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Extensions
{
    [TestClass]
    public class PrimitiveExtensionsTest
    {
        [TestMethod]
        public void InterpolateFloat()
        {
            const float start = 1.0f;
            const float end = 5.0f;
            
            Assert.AreEqual(start, start.Interpolate(end, 0.0f));
            Assert.AreEqual(2.0f, start.Interpolate(end, 0.25f));
            Assert.AreEqual(3.0f, start.Interpolate(end, 0.5f));
            Assert.AreEqual(4.0f, start.Interpolate(end, 0.75f));
            Assert.AreEqual(end, start.Interpolate(end, 1.0f));
            
            Assert.AreEqual(2.0f, end.Interpolate(start, 0.75f));
            
            Assert.AreEqual(0.0f, start.Interpolate(end, -0.25f));
            Assert.AreEqual(6.0f, start.Interpolate(end, 1.25f));
        }
        
        [TestMethod]
        public void InterpolateDouble()
        {
            const double start = 1.0;
            const double end = 5.0;
            
            Assert.AreEqual(start, start.Interpolate(end, 0.0));
            Assert.AreEqual(2.0, start.Interpolate(end, 0.25));
            Assert.AreEqual(3.0, start.Interpolate(end, 0.5));
            Assert.AreEqual(4.0, start.Interpolate(end, 0.75));
            Assert.AreEqual(end, start.Interpolate(end, 1.0));
            
            Assert.AreEqual(2.0, end.Interpolate(start, 0.75));
            
            Assert.AreEqual(0.0, start.Interpolate(end, -0.25));
            Assert.AreEqual(6.0, start.Interpolate(end, 1.25));
        }
    }
}