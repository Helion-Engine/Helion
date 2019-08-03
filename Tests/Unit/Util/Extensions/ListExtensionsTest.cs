using System.Collections.Generic;
using Helion.Util.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Extensions
{
    [TestClass]
    public class ListExtensionTest
    {
        [TestMethod]
        public void CheckIfEmpty()
        {
            Assert.IsTrue(new List<int>().Empty());
        }

        [TestMethod]
        public void CheckIfNotEmpty()
        {
            Assert.IsFalse(new List<int> { 1 }.Empty());
        }
        
        [TestMethod]
        public void CanCopyListWithoutElements()
        {
            List<int> list = new List<int>();
            IList<int> copy = list.Copy();
            
            Assert.AreNotSame(list, copy);
            Assert.IsTrue(copy.Empty());
        }
        
        [TestMethod]
        public void CanCopyListWithElements()
        {
            List<int> list = new List<int> { 0, 1, 2 };
            IList<int> copy = list.Copy();
            
            Assert.AreNotSame(list, copy);
            Assert.AreEqual(list.Count, copy.Count);
            for (int i = 0; i < list.Count; i++)
                Assert.AreEqual(i, copy[i]);
        }
    }
}