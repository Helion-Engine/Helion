using Helion.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util
{
    // Note: These are testing that we don't get a different hash value when
    // invoking it twice.
    
    [TestClass]
    public class HashCodeTest
    {
        [TestMethod]
        public void CanHashTwoElements()
        {
            Assert.AreEqual(HashCode.Combine(1, 2), HashCode.Combine(1, 2));
            Assert.AreEqual(HashCode.Combine("hi", "yes"), HashCode.Combine("hi", "yes"));
        }
        
        [TestMethod]
        public void CanHashThreeElements()
        {
            Assert.AreEqual(HashCode.Combine(1, 2, -5), HashCode.Combine(1, 2, -5));
            Assert.AreEqual(HashCode.Combine("hi", "yes", "heh"), HashCode.Combine("hi", "yes", "heh"));
        }
        
        [TestMethod]
        public void CanHashNullElements() 
        {
            string? nullStr = null;
            Assert.AreEqual(HashCode.Combine(nullStr, nullStr), HashCode.Combine(nullStr, nullStr));
            Assert.AreEqual(HashCode.Combine(nullStr, nullStr, nullStr), HashCode.Combine(nullStr, nullStr, nullStr));
        }
    }
}