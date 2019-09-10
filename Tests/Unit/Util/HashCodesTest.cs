using Helion.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util
{
    // Note: These are testing that we don't get a different hash value when
    // invoking it twice.
    
    [TestClass]
    public class HashCodesTest
    {
        [TestMethod]
        public void CanHashTwoElements()
        {
            Assert.AreEqual(HashCodes.Combine(1, 2), HashCodes.Combine(1, 2));
            Assert.AreEqual(HashCodes.Combine("hi", "yes"), HashCodes.Combine("hi", "yes"));
        }
        
        [TestMethod]
        public void CanHashThreeElements()
        {
            Assert.AreEqual(HashCodes.Combine(1, 2, -5), HashCodes.Combine(1, 2, -5));
            Assert.AreEqual(HashCodes.Combine("hi", "yes", "heh"), HashCodes.Combine("hi", "yes", "heh"));
        }
        
        [TestMethod]
        public void CanHashNullElements() 
        {
            string? nullStr = null;
            Assert.AreEqual(HashCodes.Combine(nullStr, nullStr), HashCodes.Combine(nullStr, nullStr));
            Assert.AreEqual(HashCodes.Combine(nullStr, nullStr, nullStr), HashCodes.Combine(nullStr, nullStr, nullStr));
        }
    }
}