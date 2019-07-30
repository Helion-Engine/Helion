using System.Collections.Generic;
using System.Linq;
using Helion.Util.Container;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Util.Container
{
    [TestClass]
    public class HashTableTest
    {
        [TestMethod]
        public void CanInsertAndGetByBracketNotationOrMethod()
        {
            HashTable<int, string, string> table = new HashTable<int, string, string>();
            
            Assert.IsNull(table[5, "hi"]);
            table[5, "hi"] = "hi";
            Assert.IsNotNull(table[5, "hi"]);
            Assert.AreEqual("hi", table.Get(5, "hi"));
            
            // Different first key.
            Assert.IsNull(table[4, "hi"]);
            table[4, "hi"] = "hi";
            Assert.IsNotNull(table[4, "hi"]);
            Assert.AreEqual("hi", table[4, "hi"]);
            
            // Same first key.
            Assert.IsNull(table[5, "heh"]);
            table.Insert(5, "heh", "yes");
            Assert.IsNotNull(table[5, "heh"]);
            Assert.AreEqual("yes", table[5, "heh"]);
        }

        [TestMethod]
        public void AbleToTryGet()
        {
            HashTable<int, string, string> table = new HashTable<int, string, string>();
            
            table[5, "hi"] = "yes";

            string? value = null;
            bool found = table.TryGet(5, "hi", ref value);
            Assert.IsTrue(found);
            Assert.AreEqual("yes", value);
            
            value = null;
            found = table.TryGet(5, "heh", ref value);
            Assert.IsFalse(found);
            Assert.IsNull(value);
            
            value = null;
            found = table.TryGet(4, "hi", ref value);
            Assert.IsFalse(found);
            Assert.IsNull(value);
        }

        [TestMethod]
        public void CanClear()
        {
            HashTable<int, string, string> table = new HashTable<int, string, string>();
            
            table[5, "hi"] = "hi";
            Assert.IsNotNull(table[5, "hi"]);
            
            table.Clear();
            Assert.IsNull(table[5, "hi"]);
        }

        [TestMethod]
        public void RemoveKeyPair()
        {
            HashTable<int, string, string> table = new HashTable<int, string, string>();
            
            table[5, "hi"] = "hi";
            table[5, "yes"] = "yes";
            Assert.IsNotNull(table[5, "hi"]);
            Assert.IsNotNull(table[5, "yes"]);
            
            bool removed = table.Remove(5, "hi");
            Assert.IsNull(table[5, "hi"]);
            Assert.IsTrue(removed);
            
            // Removing a value that doesn't exist should neither crash, nor
            // should it touch anything else in the process of failing.
            removed = table.Remove(5, "hi");
            Assert.IsNull(table[5, "hi"]);
            Assert.IsNotNull(table[5, "yes"]);
            Assert.IsFalse(removed);
        }
        
        [TestMethod]
        public void GetFirstKeys()
        {
            HashTable<int, string, string> table = new HashTable<int, string, string>();
            
            table[5, "hi"] = "hi";
            table[5, "a"] = "a";
            table[3, ""] = "test";

            IEnumerable<int> keys = table.GetFirstKeys();
            Assert.AreEqual(2, keys.Count());

            HashSet<int> keySet = keys.ToHashSet();
            Assert.IsTrue(keySet.Contains(3));
            Assert.IsTrue(keySet.Contains(5));
        }
    }
}