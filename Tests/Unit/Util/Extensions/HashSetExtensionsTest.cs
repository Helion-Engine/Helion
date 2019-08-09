using System.Collections.Generic;
using Helion.Util.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Extensions
{
    [TestClass]
    public class HashSetExtensionsTest
    {
        [TestMethod]
        public void CheckIfEmpty()
        {
            Assert.IsTrue(new HashSet<int>().Empty());
        }
        
        [TestMethod]
        public void CheckIfNotEmpty()
        {
            Assert.IsFalse(new HashSet<int> { 1 }.Empty());
            Assert.IsFalse(new HashSet<int> { 1, 2, 3 }.Empty());
        }
    }
}