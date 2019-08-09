using Helion.Util.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Extensions
{
    [TestClass]
    public class ArrayExtensionsTest
    {
        [TestMethod]
        public void FillWith()
        {
            int[] array = { 0, 1, 2, 3, 4, 5 };
            
            array.Fill(17);
            foreach (int value in array)
                Assert.AreEqual(17, value);
        }
    }
}