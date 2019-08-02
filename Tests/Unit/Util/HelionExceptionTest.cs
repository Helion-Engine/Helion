using Helion.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Helion.Test.Unit.Util
{
    [TestClass]
    public class ExceptionTest
    {
        [TestMethod]
        public void CreateHelionException()
        {
            string msg = "hi";
            HelionException exception = new HelionException(msg);
            
            Assert.AreEqual(msg, exception.Message);
        }
    }
}