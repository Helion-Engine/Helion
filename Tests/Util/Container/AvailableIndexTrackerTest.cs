using Helion.Util.Container;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Util.Container
{
    [TestClass]
    public class AvailableIndexTrackerTest
    {
        [TestMethod]
        public void GetNextIndexFromNewObject()
        {
            AvailableIndexTracker tracker = new AvailableIndexTracker();

            Assert.AreEqual(0, tracker.Length);
            
            Assert.AreEqual(0, tracker.Next());
            
            Assert.AreEqual(1, tracker.Length);
        }
        
        [TestMethod]
        public void GetMultipleNextIndexFromNewObject()
        {
            AvailableIndexTracker tracker = new AvailableIndexTracker();

            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(i, tracker.Next());
            Assert.AreEqual(1000, tracker.Length);
        }

        [TestMethod]
        public void ReturnValuesAndGetThemAgain()
        {
            AvailableIndexTracker tracker = new AvailableIndexTracker();

            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i, tracker.Next());
            
            tracker.MakeAvailable(3);
            Assert.AreEqual(3, tracker.Next());
            Assert.AreEqual(10, tracker.Next());
            Assert.AreEqual(11, tracker.Next());
        }

        [TestMethod]
        public void ReturnEndpointsAndGetThemAgain()
        {
            AvailableIndexTracker tracker = new AvailableIndexTracker();

            for (int i = 0; i < 5; i++)
                Assert.AreEqual(i, tracker.Next());
            Assert.AreEqual(5, tracker.Length);
            
            tracker.MakeAvailable(4);
            Assert.AreEqual(4, tracker.Length);
            
            tracker.MakeAvailable(3);
            Assert.AreEqual(3, tracker.Length);
            
            Assert.AreEqual(3, tracker.Next());
            Assert.AreEqual(4, tracker.Next());
            Assert.AreEqual(5, tracker.Next());
            Assert.AreEqual(6, tracker.Length);
        }
    }
}