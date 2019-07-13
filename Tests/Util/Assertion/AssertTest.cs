using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Util.Assertion
{
    [TestClass]
    public class AssertTest
    {
        [TestMethod]
        public void CanPassPrecondition()
        {
            Helion.Util.Assertion.Assert.Precondition(true, "no message");
        }
        
        [TestMethod]
        public void CanFailPrecondition()
        {
            const string errorMsg = "no message";

            try
            {
                Helion.Util.Assertion.Assert.Precondition(false, errorMsg);
                Assert.Fail("Should not have passed the precondition without throwing");
            }
            catch (Helion.Util.Assertion.AssertionException e)
            {
                Assert.AreEqual(errorMsg, e.Message);
            }
        }
        
        [TestMethod]
        public void CanPassInvariant()
        {
            Helion.Util.Assertion.Assert.Invariant(true, "no message");
        }
        
        [TestMethod]
        public void CanFailInvariant()
        {
            const string errorMsg = "precondition message";

            try
            {
                Helion.Util.Assertion.Assert.Invariant(false, errorMsg);
                Assert.Fail("Should not have passed the invariant without throwing");
            }
            catch (Helion.Util.Assertion.AssertionException e)
            {
                Assert.AreEqual(errorMsg, e.Message);
            }
        }
        
        [TestMethod]
        public void CanPassPostcondition()
        {
            Helion.Util.Assertion.Assert.Postcondition(true, "invariant message");
        }
        
        [TestMethod]
        public void CanFailPostcondition()
        {
            const string errorMsg = "postcondition message";

            try
            {
                Helion.Util.Assertion.Assert.Postcondition(false, errorMsg);
                Assert.Fail("Should not have passed the postcondition without throwing");
            }
            catch (Helion.Util.Assertion.AssertionException e)
            {
                Assert.AreEqual(errorMsg, e.Message);
            }
        }
        
        [TestMethod]
        public void CanInvokeFailure()
        {
            const string errorMsg = "fail message";

            try
            {
                Helion.Util.Assertion.Assert.Fail(errorMsg);
                Assert.Fail("Fail() should always throw when invoked");
            }
            catch (Helion.Util.Assertion.AssertionException e)
            {
                Assert.AreEqual(errorMsg, e.Message);
            }
        }
    }
}