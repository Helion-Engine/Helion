using System.Collections.Generic;
using Helion.Util.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Extensions
{
    [TestClass]
    public class LinkedListExtensionsTest
    {
        [TestMethod]
        public void CheckIfEmpty()
        {
            Assert.IsTrue(new LinkedList<int>().Empty());
        }

        [TestMethod]
        public void CheckIfNotEmpty()
        {
            LinkedList<int> list = new LinkedList<int>();
            list.AddLast(1);
            Assert.IsFalse(list.Empty());
        }
    }
}