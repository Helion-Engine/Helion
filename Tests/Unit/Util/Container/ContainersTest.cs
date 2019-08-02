using System.Collections.Generic;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Container
{
    [TestClass]
    public class ContainersTest
    {
        [TestMethod]
        public void ListWithAllNulls()
        {
            Assert.IsTrue(Containers.WithoutNulls<string>(null, null, null).Empty());
        }
        
        [TestMethod]
        public void ListWithSomeNulls()
        {
            IList<string> list = Containers.WithoutNulls<string>(null, "hello", null, "yes");
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("hello", list[0]);
            Assert.AreEqual("yes", list[1]);
        }
        
        [TestMethod]
        public void ListWithNoNulls()
        {
            IList<string> list = Containers.WithoutNulls<string>("test", "hello", "", "yes", "");
            Assert.AreEqual(5, list.Count);
            Assert.AreEqual("test", list[0]);
            Assert.AreEqual("hello", list[1]);
            Assert.AreEqual("", list[2]);
            Assert.AreEqual("yes", list[3]);
            Assert.AreEqual("", list[4]);
        }
    }
}