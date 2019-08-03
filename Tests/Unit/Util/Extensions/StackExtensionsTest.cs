using System.Collections.Generic;
using Helion.Util.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Extensions
{
    [TestClass]
    public class StackExtensionsTest
    {
        [TestMethod]
        public void EmptyStackIsEmpty()
        {
            Assert.IsTrue(new Stack<int>().Empty());
        }
        
        [TestMethod]
        public void NonEmptyStackIsNotEmpty()
        {
            Stack<int> stack = new Stack<int>();
            
            stack.Push(1);
            Assert.IsFalse(stack.Empty());
            
            stack.Push(1582);
            Assert.IsFalse(stack.Empty());
            
            stack.Clear();
            Assert.IsTrue(stack.Empty());
            
            stack.Push(17);
            Assert.IsFalse(stack.Empty());
        }

        [TestMethod]
        public void TryPeekTopElementInEmptyStack()
        {
            Assert.IsFalse(new Stack<int>().TryPeek(out int _));
        }
        
        [TestMethod]
        public void TryPeekTopElementInNonEmptyStack()
        {
            Stack<int> stack = new Stack<int>();
            stack.Push(5);
            stack.Push(8);
            
            Assert.IsTrue(stack.TryPeek(out int value));
            Assert.AreEqual(8, value);
        }
    }
}