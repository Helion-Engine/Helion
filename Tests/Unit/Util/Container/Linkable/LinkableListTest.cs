using Helion.Util.Container.Linkable;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Container.Linkable
{
    [TestClass]
    public class LinkableListTest
    {
        private static void AssertListSequence<T>(LinkableList<T> list, params T[] values)
        {
            int iteration = 0;
            foreach (T value in list)
            {
                Assert.AreEqual(values[iteration], value);
                iteration++;
            }
            
            if (iteration != values.Length)
                Assert.Fail("List contains too many or not enough values");
        }
        
        [TestMethod]
        public void CreateEmptyList()
        {
            LinkableList<int> list = new LinkableList<int>();
            
            foreach (int i in list)
                Assert.Fail();
        }
        
        [TestMethod]
        public void CanAddToList()
        {
            LinkableList<int> list = new LinkableList<int>();
            Assert.IsNull(list.Head);
            
            LinkableNode<int> tail = list.Add(5);
            Assert.AreEqual(5, tail.Value);
            Assert.AreSame(tail, list.Head);
            AssertListSequence(list, 5);

            LinkableNode<int> head = list.Add(8);
            Assert.AreEqual(5, tail.Value);
            Assert.AreEqual(8, head.Value);
            Assert.AreSame(head, list.Head);
            AssertListSequence(list, 8, 5);
            
            Assert.AreSame(tail, head.Next);
            Assert.IsNull(tail.Next);
        }

        [TestMethod]
        public void CanUnlinkLast()
        {
            LinkableList<int> list = new LinkableList<int>();
            LinkableNode<int> tail = list.Add(1);
            LinkableNode<int> middle = list.Add(2);
            LinkableNode<int> front = list.Add(3);
            AssertListSequence(list, 3, 2, 1);
            
            tail.Unlink();
            Assert.AreEqual(2, middle.Value);
            Assert.AreEqual(3, front.Value);
            Assert.AreSame(front, list.Head);
            Assert.IsNull(middle.Next);
            AssertListSequence(list, 3, 2);
        }
        
        [TestMethod]
        public void CanUnlinkMiddle()
        {
            LinkableList<int> list = new LinkableList<int>();
            LinkableNode<int> tail = list.Add(1);
            LinkableNode<int> middle = list.Add(2);
            LinkableNode<int> front = list.Add(3);
            AssertListSequence(list, 3, 2, 1);
            
            middle.Unlink();
            Assert.AreEqual(1, tail.Value);
            Assert.AreEqual(3, front.Value);
            Assert.IsNull(tail.Next);
            Assert.AreSame(front, list.Head);
            Assert.AreSame(tail, front.Next);
            AssertListSequence(list, 3, 1);
        }
        
        [TestMethod]
        public void CanUnlinkFirst()
        {
            LinkableList<int> list = new LinkableList<int>();
            LinkableNode<int> tail = list.Add(1);
            LinkableNode<int> middle = list.Add(2);
            LinkableNode<int> front = list.Add(3);
            AssertListSequence(list, 3, 2, 1);
            
            front.Unlink();
            Assert.AreEqual(1, tail.Value);
            Assert.AreEqual(2, middle.Value);
            Assert.IsNull(tail.Next);
            Assert.AreSame(tail, middle.Next);
            AssertListSequence(list, 2, 1);
        }

        [TestMethod]
        public void CanLinkAndUnlink()
        {
            // [1] +> [2, 1] +> [3, 2, 1] -> [3, 1] +> [4, 3, 1] -> [3, 1] -> [3] +> [5, 3] --> [] +> [6] -> []
            LinkableList<int> list = new LinkableList<int>();
            LinkableNode<int> one = list.Add(1);
            LinkableNode<int> two = list.Add(2);
            LinkableNode<int> three = list.Add(3);
            Assert.AreSame(three, list.Head);
            AssertListSequence(list, 3, 2, 1);

            two.Unlink();
            Assert.AreSame(three, list.Head);
            AssertListSequence(list, 3, 1);

            LinkableNode<int> four = list.Add(4);
            Assert.AreSame(four, list.Head);
            AssertListSequence(list, 4, 3, 1);

            four.Unlink();
            Assert.AreSame(three, list.Head);
            AssertListSequence(list, 3, 1);

            one.Unlink();
            Assert.AreSame(three, list.Head);
            AssertListSequence(list, 3);

            LinkableNode<int> five = list.Add(5);
            Assert.AreSame(five, list.Head);
            AssertListSequence(list, 5, 3);

            three.Unlink();
            five.Unlink();
            Assert.IsNull(list.Head);
            AssertListSequence(list);

            LinkableNode<int> six = list.Add(6);
            Assert.AreSame(six, list.Head);
            AssertListSequence(list, 6);

            six.Unlink();
            Assert.IsNull(list.Head);
            AssertListSequence(list);
        }
    }
}