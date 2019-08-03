using System.Collections.Generic;
using Helion.Util.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Extensions
{
    [TestClass]
    public class DictionaryExtensionsTest
    {
        [TestMethod]
        public void GetDefaultValueWhenKeyMissing()
        {
            const string defaultValue = "default";
            Dictionary<int, string> dictionary = new Dictionary<int, string> { [1] = "hi", [17] = "17!" };

            Assert.AreEqual(defaultValue, dictionary.GetValueOrDefault(3, defaultValue));
        }
        
        [TestMethod]
        public void GetStoredValueWhenKeyPresent()
        {
            Dictionary<int, string> dictionary = new Dictionary<int, string> { [1] = "hi", [17] = "17!" };

            Assert.AreEqual("hi", dictionary.GetValueOrDefault(1, "default"));
        }
    }
}