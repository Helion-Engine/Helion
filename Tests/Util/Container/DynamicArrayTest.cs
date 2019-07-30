using Helion.Util.Container;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Util.Container
{
    [TestClass]
    public class DynamicArrayTest
    {
        [TestMethod]
        public void CreateDynamicArray()
        {
            DynamicArray<int> array = new DynamicArray<int>();
            Assert.AreEqual(0, array.Length);
        }
        
        [TestMethod]
        public void CanAddElements()
        {
            DynamicArray<int> array = new DynamicArray<int>(4);
            Assert.AreEqual(4, array.Capacity);
            
            array.Add(5);
            Assert.AreEqual(4, array.Capacity);
            Assert.AreEqual(1, array.Length);
            Assert.AreEqual(5, array[0]);
            Assert.AreEqual(5, array.Data[0]);
            
            array.Add(4, -3);
            Assert.AreEqual(4, array.Capacity);
            Assert.AreEqual(3, array.Length);
            Assert.AreEqual(5, array[0]);
            Assert.AreEqual(5, array.Data[0]);
            Assert.AreEqual(4, array[1]);
            Assert.AreEqual(4, array.Data[1]);
            Assert.AreEqual(-3, array[2]);
            Assert.AreEqual(-3, array.Data[2]);
            
            array.Add(42);
            Assert.AreEqual(4, array.Capacity);
            Assert.AreEqual(4, array.Length);
            Assert.AreEqual(5, array[0]);
            Assert.AreEqual(5, array.Data[0]);
            Assert.AreEqual(4, array[1]);
            Assert.AreEqual(4, array.Data[1]);
            Assert.AreEqual(-3, array[2]);
            Assert.AreEqual(-3, array.Data[2]);
            Assert.AreEqual(42, array[3]);
            Assert.AreEqual(42, array.Data[3]);
            
            array.Add(12345);
            Assert.AreEqual(8, array.Capacity);
            Assert.AreEqual(5, array.Length);
            Assert.AreEqual(5, array[0]);
            Assert.AreEqual(5, array.Data[0]);
            Assert.AreEqual(4, array[1]);
            Assert.AreEqual(4, array.Data[1]);
            Assert.AreEqual(-3, array[2]);
            Assert.AreEqual(-3, array.Data[2]);
            Assert.AreEqual(42, array[3]);
            Assert.AreEqual(42, array.Data[3]);
            Assert.AreEqual(12345, array[4]);
            Assert.AreEqual(12345, array.Data[4]);
        }
        
        [TestMethod]
        public void PerformsIteration()
        {
            int[] numbers = { 1, 2, 3, 4, 5 };
            
            DynamicArray<int> array = new DynamicArray<int>();
            array.Add(numbers);

            int currentIndex = 0;
            foreach (int val in array)
                Assert.AreEqual(numbers[currentIndex++], val);
            Assert.AreEqual(array.Length, currentIndex);
        }

        [TestMethod]
        public void CanClear()
        {
            DynamicArray<int> array = new DynamicArray<int>(4);
            array.Add(1, 2, 3, 4, 5);

            Assert.AreEqual(5, array.Length);
            array.Clear();
            Assert.AreEqual(0, array.Length);

            // The data should not be cleared though.
            for (int i = 0; i < 5; i++)
                Assert.AreEqual(i + 1, array[i]);
        }
    }
}