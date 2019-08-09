using Helion.Util.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Extensions
{
    [TestClass]
    public class StringExtensionsTest
    {
        [TestMethod]
        public void EmptyStringIsEmpty()
        {
            Assert.IsTrue("".Empty());
        }
        
        [TestMethod]
        public void NonEmptyStringIsNotEmpty()
        {
            Assert.IsFalse("hi".Empty());
            Assert.IsFalse(" ".Empty());
            Assert.IsFalse("a b c aisdu asd90  _ASD-asd,.123456789[]/\\yes".Empty());
        }
    }
}