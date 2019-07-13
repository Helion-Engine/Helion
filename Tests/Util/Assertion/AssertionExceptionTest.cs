using Helion.Util.Assertion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Helion.Test.Util.Assertion
{
    [TestClass]
    public class AssertionExceptionTest
    {
        [TestMethod]
        public void CreateAssertionException()
        {
            string msg = "hi";
            AssertionException exception = new AssertionException(msg);
            
            Assert.AreEqual(msg, exception.Message);
        }
    }
}